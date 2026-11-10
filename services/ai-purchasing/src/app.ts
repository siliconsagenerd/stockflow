import express from "express";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { countPendingDuplicates, listPendingRequests, listWorkspaces, loadHistory } from "./db.js";
import { recommend, type LlmConfig } from "./llm.js";
import type { Queryable, Recommendation } from "./types.js";

export interface AppDeps {
  db: Queryable;
  llm: LlmConfig | null;
  webRoot?: string;
}

const here = path.dirname(fileURLToPath(import.meta.url));

export function createApp({ db, llm, webRoot }: AppDeps) {
  const app = express();
  app.disable("x-powered-by");
  app.use(express.json({ limit: "32kb" }));

  const withRecommendations = async (workspaceId: string | null) => {
    const requests = await listPendingRequests(db, workspaceId);
    const history = await loadHistory(db, [...new Set(requests.map((r) => r.productId))]);
    const duplicates = await countPendingDuplicates(db, requests);
    const items = await Promise.all(
      requests.map(async (request) => ({
        request,
        recommendation: await recommend(
          request,
          history.get(request.productId),
          duplicates.get(request.productId) ?? 0,
          llm,
        ),
      })),
    );
    return items;
  };

  app.get("/api/health", async (_req, res) => {
    await db.query("SELECT 1");
    res.json({ status: "ok", engine: llm ? llm.model : "rule-engine" });
  });

  app.get("/api/workspaces", async (_req, res) => {
    res.json({ items: await listWorkspaces(db) });
  });

  app.get("/api/queue", async (req, res) => {
    const workspaceId =
      typeof req.query.workspaceId === "string" && req.query.workspaceId !== ""
        ? req.query.workspaceId
        : null;
    const items = await withRecommendations(workspaceId);
    const first = items[0]?.recommendation as Recommendation | undefined;
    res.json({
      workspaceId,
      generatedAt: new Date().toISOString(),
      source: first?.source ?? (llm ? "llm" : "rules"),
      model: first?.model ?? (llm ? llm.model : "rule-engine"),
      items,
    });
  });

  app.get("/api/requests/:id/recommendation", async (req, res) => {
    const items = await withRecommendations(null);
    const hit = items.find((item) => item.request.id === req.params.id);
    if (!hit) {
      res.status(404).json({ error: "request not found or not pending" });
      return;
    }
    res.json(hit);
  });

  if (webRoot && fs.existsSync(path.join(webRoot, "index.html"))) {
    app.use(express.static(webRoot));
    app.get(/^\/$/, (_req, res) => res.sendFile(path.join(webRoot, "index.html")));
  }

  app.use((err: Error, _req: express.Request, res: express.Response, _next: express.NextFunction) => {
    console.error(err);
    res.status(500).json({ error: "internal error" });
  });


  return app;
}

export const defaultWebRoot = path.resolve(here, "../web");
