import assert from "node:assert/strict";
import test from "node:test";
import type { AddressInfo } from "node:net";
import { createApp } from "../src/app.js";
import { createDemoDatabase } from "../src/demo.js";

async function startApi() {
  const demo = await createDemoDatabase();
  const app = createApp({ db: demo.db, llm: null });
  const server = app.listen(0);
  await new Promise((resolve) => server.once("listening", resolve));
  const { port } = server.address() as AddressInfo;
  return {
    base: `http://127.0.0.1:${port}`,
    close: async () => {
      await new Promise((resolve) => server.close(resolve));
      await demo.close();
    },
  };
}

test("queue returns pending requests with recommendations", async () => {
  const api = await startApi();
  try {
    const response = await fetch(`${api.base}/api/queue`);
    assert.equal(response.status, 200);
    const body = (await response.json()) as {
      source: string;
      items: { request: { sku: string }; recommendation: { score: number; decision: string; source: string } }[];
    };
    assert.equal(body.source, "rules");
    assert.equal(body.items.length, 4);

    const filter = body.items.find((item) => item.request.sku === "FLT-0140");
    assert.ok(filter);
    assert.equal(filter!.recommendation.decision, "approve");
    assert.equal(filter!.recommendation.source, "rules");

    const valve = body.items.find((item) => item.request.sku === "VLV-0025");
    assert.ok(valve);
    assert.ok(valve!.recommendation.score < 70, "oversized quantity must not be auto-approved");
  } finally {
    await api.close();
  }
});

test("queue is ordered by age, oldest first", async () => {
  const api = await startApi();
  try {
    const body = (await (await fetch(`${api.base}/api/queue`)).json()) as {
      items: { request: { createdAt: string } }[];
    };
    const timestamps = body.items.map((item) => new Date(item.request.createdAt).getTime());
    assert.deepEqual(timestamps, [...timestamps].sort((a, b) => a - b));
  } finally {
    await api.close();
  }
});

test("single recommendation endpoint returns the matching request", async () => {
  const api = await startApi();
  try {
    const queue = (await (await fetch(`${api.base}/api/queue`)).json()) as {
      items: { request: { id: string } }[];
    };
    const id = queue.items[0]!.request.id;
    const response = await fetch(`${api.base}/api/requests/${id}/recommendation`);
    assert.equal(response.status, 200);
    const body = (await response.json()) as { recommendation: { requestId: string } };
    assert.equal(body.recommendation.requestId, id);
  } finally {
    await api.close();
  }
});

test("unknown request id returns 404", async () => {
  const api = await startApi();
  try {
    const response = await fetch(
      `${api.base}/api/requests/00000000-0000-4000-8000-000000000000/recommendation`,
    );
    assert.equal(response.status, 404);
  } finally {
    await api.close();
  }
});

test("health reports the active engine", async () => {
  const api = await startApi();
  try {
    const body = (await (await fetch(`${api.base}/api/health`)).json()) as { status: string; engine: string };
    assert.equal(body.status, "ok");
    assert.equal(body.engine, "rule-engine");
  } finally {
    await api.close();
  }
});
