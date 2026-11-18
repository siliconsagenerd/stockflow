export interface PendingRequest {
  id: string;
  workspaceId: string;
  productId: string;
  productName: string;
  sku: string;
  unit: string;
  quantity: number;
  justification: string | null;
  requester: string;
  createdAt: string;
  currentStock: number;
}

export interface ProductHistory {
  productId: string;
  sampleSize: number;
  averageQuantity: number;
  rejectedCount: number;
}

export interface Signals {
  stockCoverage: number;
  quantityRatio: number | null;
  justificationLength: number;
  duplicatePending: number;
  historyRejectionRate: number | null;
  historySamples: number;
}

export type Decision = "approve" | "review" | "reject";

export interface Recommendation {
  requestId: string;
  score: number;
  decision: Decision;
  confidence: number;
  rationale: string[];
  risks: string[];
  signals: Signals;
  source: "rules" | "llm";
  model: string;
}

export interface Queryable {
  query<R = Record<string, unknown>>(
    sql: string,
    params?: unknown[],
  ): Promise<{ rows: R[] }>;
}
