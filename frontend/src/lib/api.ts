const WORKSPACE_KEY = "stockflow.workspaceId";
const IDENTITY_KEY = "stockflow.demoUserId";

export function getWorkspaceId(): string | null {
  return localStorage.getItem(WORKSPACE_KEY);
}
export function setWorkspaceId(id: string) {
  localStorage.setItem(WORKSPACE_KEY, id);
}
export function getDemoUserId(): string | null {
  return localStorage.getItem(IDENTITY_KEY);
}
export function setDemoUserId(id: string) {
  localStorage.setItem(IDENTITY_KEY, id);
}

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers || {});
  const ws = getWorkspaceId();
  if (ws) headers.set("X-Demo-Workspace", ws);
  const user = getDemoUserId();
  if (user) headers.set("X-Demo-User", user);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  return fetch(path, { ...init, headers });
}

export async function ensureWorkspace(): Promise<string> {
  const existing = getWorkspaceId();
  if (existing) return existing;
  const res = await fetch("/api/workspaces", { method: "POST" });
  if (!res.ok) throw new Error("Could not create demo workspace");
  const data = await res.json();
  setWorkspaceId(data.id);
  return data.id;
}