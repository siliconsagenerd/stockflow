# Architecture — StockFlow

## Components
- ASP.NET Core Web API (.NET 10) — controllers, EF Core DbContext, seed, health.
- PostgreSQL 16 — schema + data.
- Vue 3 + TS (Vite) — SPA, proxies /api and /health to API.

## Data model
Product, Supplier, Warehouse, DemoUser, DemoWorkspace, DemoSession, PurchaseRequest, PurchaseOrder, GoodsReceipt, StockMovement.
Constraints: unique Sku, unique PurchaseOrder.PurchaseRequestId, unique GoodsReceipt.IdempotencyKey, unique DemoSession.TokenHash.
PurchaseRequest has WorkspaceId FK (cascade delete) for visitor isolation.

## Anonymous session (Phase 2 hardening)
- Visitor's browser calls POST /api/session once; server creates a DemoWorkspace + DemoSession and sets an HttpOnly, SameSite=Strict cookie (`stockflow_session`) containing a 32-byte random token (base64url). Only a SHA-256 hash of the token is stored server-side.
- All request reads/writes resolve the workspace from the validated session cookie. Caller-supplied workspace IDs (X-Demo-Workspace) are ignored as access selectors.
- Cookie: HttpOnly, Secure in production (HTTPS), SameSite=Strict, Path=/, 8-hour expiry. Local HTTP dev uses Secure=false (explicit dev setting).
- CSRF protection: SameSite=Strict cookie + JSON Content-Type (triggers CORS preflight) for mutations. Proportionate for a demo.
- GET /api/workspaces/{id} follows the same isolation: requires a valid session whose workspace matches; otherwise 404/401.
- Limitations (documented): anonymous session possession grants access to its demo workspace; stolen session tokens remain a risk; this is not verified user identity. Demo identity switching remains simulated (X-Demo-User header).

## Inventory scope decision (Phase 4 input)
- Shared product metadata (name, SKU, unit, preferred supplier) is read-only and common to all workspaces.
- Mutable currentStock CANNOT be a single shared balance while workflow records are isolated.
- Selected approach: visitor-scoped stock balances. Each DemoWorkspace owns its own StockBalance rows keyed by (WorkspaceId, ProductId, WarehouseId). Receipts (Phase 4) will update the workspace's own balances, not a shared one.
- Implementation deferred to Phase 4.

## API endpoints (Phase 2)
- POST /api/session — create anonymous session + workspace, set cookie
- GET /api/session — current session info
- DELETE /api/session — clear session
- POST /api/workspaces — alias (creates session)
- GET /api/workspaces/{id} — workspace detail (session-scoped)
- GET /api/products — shared catalog
- GET /api/demo-users — shared identities
- GET /api/requests — list (session-scoped)
- GET /api/requests/{id} — detail (session-scoped)
- POST /api/requests — create Pending request

## Transaction boundaries (later phases)
- Approval + PO creation: single transaction.
- Receipt + stock update + movement: single transaction.
- Idempotency: repeated same key+payload returns existing outcome; conflicting payload rejected.

## Health
`GET /health` combines process liveness and database readiness (CanConnect).
Returns 200 Healthy / 503 Unhealthy with JSON body.

## Dev networking
- API: oldmac 127.0.0.1:5050
- Vite: oldmac 127.0.0.1:5173 (loopback only; M2 via SSH tunnel)
- Postgres: oldmac 192.168.3.146:5432 (existing container, dedicated DB/role)
- M2 access: `ssh -f -N -L 5173:127.0.0.1:5173 -L 5050:127.0.0.1:5050 oldmac`

## Known limitation
MSB3277: EF Core 10.0.13 vs Npgsql 10.0.3 transitive EF Core Relational 10.0.4 (no matching standalone version available). Non-blocking; tracked separately. Cause not settled without dependency-resolution evidence.
