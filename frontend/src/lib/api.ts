import { ref } from "vue";
const IDENTITY_KEY = "stockflow.demoUserId";
// reactive so identity switches update computeds (isManager) without a reload
const demoUserId = ref<string | null>(localStorage.getItem(IDENTITY_KEY));
export function getDemoUserId(): string | null {
  return demoUserId.value;
}
export function setDemoUserId(id: string) {
  demoUserId.value = id;
  localStorage.setItem(IDENTITY_KEY, id);
}

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const headers = new Headers(init.headers || {});
  const user = getDemoUserId();
  if (user) headers.set("X-Demo-User", user);
  if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
  return fetch(path, { ...init, headers, credentials: "include" });
}

export async function ensureSession(): Promise<string> {
  // Try to reuse an existing session cookie first.
  try {
    const res = await fetch("/api/session", { credentials: "include" });
    if (res.ok) {
      const data = await res.json();
      return data.workspaceId;
    }
  } catch { /* fall through */ }
  const res = await fetch("/api/session", { method: "POST", credentials: "include" });
  if (!res.ok) throw new Error("Could not create demo session");
  const data = await res.json();
  return data.workspaceId;
}
