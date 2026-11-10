export type Decision = "approve" | "review" | "reject";

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

export interface Signals {
  stockCoverage: number;
  quantityRatio: number | null;
  justificationLength: number;
  duplicatePending: number;
  historyRejectionRate: number | null;
  historySamples: number;
}

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

export interface QueueItem {
  request: PendingRequest;
  recommendation: Recommendation;
}

export interface QueueResponse {
  workspaceId: string | null;
  generatedAt: string;
  source: "rules" | "llm";
  model: string;
  items: QueueItem[];
}

export async function fetchQueue(workspaceId?: string | null): Promise<QueueResponse> {
  const query = workspaceId ? `?workspaceId=${encodeURIComponent(workspaceId)}` : "";
  const response = await fetch(`/api/queue${query}`, { headers: { accept: "application/json" } });
  if (!response.ok) throw new Error(`queue failed: ${response.status}`);
  return (await response.json()) as QueueResponse;
}
