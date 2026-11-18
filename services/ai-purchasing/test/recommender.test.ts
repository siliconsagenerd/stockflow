import assert from "node:assert/strict";
import test from "node:test";
import { buildSignals, scoreRequest } from "../src/recommender.js";
import type { PendingRequest, ProductHistory } from "../src/types.js";

const request = (overrides: Partial<PendingRequest> = {}): PendingRequest => ({
  id: "req-1",
  workspaceId: "ws-1",
  productId: "p-1",
  productName: "Filterpatrone",
  sku: "FLT-0140",
  unit: "Stk",
  quantity: 40,
  justification: "Linie 3 steht, Filter sind leer und der Service ist am Donnerstag.",
  requester: "Anna Beispiel",
  createdAt: new Date().toISOString(),
  currentStock: 0,
  ...overrides,
});

const history = (overrides: Partial<ProductHistory> = {}): ProductHistory => ({
  productId: "p-1",
  sampleSize: 3,
  averageQuantity: 40,
  rejectedCount: 0,
  ...overrides,
});

test("zero stock with matching history recommends approval", () => {
  const result = scoreRequest(request(), history(), 0);
  assert.equal(result.decision, "approve");
  assert.ok(result.score >= 70);
  assert.deepEqual(result.risks, []);
});

test("quantity far above history is flagged and demoted", () => {
  const result = scoreRequest(request({ quantity: 200 }), history(), 0);
  assert.ok(result.score < 70);
  assert.equal(result.risks.length, 1);
  assert.match(result.risks[0]!, /hoch wie der Durchschnitt/);
});

test("thin justification counts as a risk", () => {
  const result = scoreRequest(request({ justification: "Reserve" }), history(), 0);
  assert.ok(result.risks.some((line) => line.includes("kuerzer als 20 Zeichen")));
});

test("duplicate pending requests lower the score by ten", () => {
  const base = scoreRequest(request(), history(), 0).score;
  const withDuplicate = scoreRequest(request(), history(), 1).score;
  assert.equal(base - withDuplicate, 10);
});

test("stacked penalties demote to reject", () => {
  const result = scoreRequest(
    request({ quantity: 999, justification: "kurz", currentStock: 400 }),
    history({ rejectedCount: 3 }),
    3,
  );
  assert.equal(result.decision, "reject");
  assert.ok(result.score < 40);
  assert.ok(result.risks.length >= 4);
});

test("score never leaves the 0 to 100 range", () => {
  const worst = scoreRequest(
    request({ quantity: 999, justification: "", currentStock: 400 }),
    history({ rejectedCount: 3 }),
    3,
  );
  assert.ok(worst.score >= 0 && worst.score <= 100);
  const best = scoreRequest(request(), history(), 0);
  assert.ok(best.score >= 0 && best.score <= 100);
  assert.ok(worst.score < best.score);
});

test("quantity ratio is only computed with at least three samples", () => {
  const thin = buildSignals(request(), history({ sampleSize: 2 }), 0);
  assert.equal(thin.quantityRatio, null);
  const solid = buildSignals(request(), history(), 0);
  assert.equal(solid.quantityRatio, 1);
});

test("confidence rises with available data", () => {
  const sparse = scoreRequest(request(), undefined, 0).confidence;
  const rich = scoreRequest(request(), history(), 0).confidence;
  assert.ok(rich > sparse);
  assert.ok(rich <= 0.95);
});
