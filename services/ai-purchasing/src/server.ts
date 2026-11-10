import pg from "pg";
import { createApp, defaultWebRoot } from "./app.js";
import { resolveLlmConfig } from "./llm.js";
import { createDemoDatabase } from "./demo.js";
import type { Queryable } from "./types.js";

const port = Number(process.env.PORT ?? 8099);
const llm = resolveLlmConfig(process.env);

let db: Queryable;
let close: () => Promise<void> = async () => {};

if (process.env.DEMO_MODE === "1") {
  const demo = await createDemoDatabase();
  db = demo.db;
  close = demo.close;
  console.log("DEMO_MODE: in-memory PGlite with seed data");
} else {
  const pool = new pg.Pool({
    connectionString:
      process.env.DATABASE_URL ??
      "postgres://stockflow:stockflow@127.0.0.1:5432/stockflow",
    max: 5,
    application_name: "stockflow-ai-purchasing",
  });
  db = pool;
  close = async () => {
    await pool.end();
  };
}

const app = createApp({ db, llm, webRoot: defaultWebRoot });
const server = app.listen(port, () => {
  console.log(`ai-purchasing on http://127.0.0.1:${port} (${llm ? llm.model : "rule-engine"})`);
});

const shutdown = async () => {
  server.close();
  await close();
  process.exit(0);
};
process.on("SIGTERM", shutdown);
process.on("SIGINT", shutdown);
