# Architecture — StockFlow

## Components
- ASP.NET Core Web API (.NET 10) — controllers, EF Core DbContext, seed, health.
- PostgreSQL 16 — schema + data.
- Vue 3 + TS (Vite) — SPA, proxies /api and /health to API.

## Data model
Product, Supplier, Warehouse, DemoUser, DemoWorkspace, PurchaseRequest, PurchaseOrder, GoodsReceipt, StockMovement.
Constraints: unique Sku, unique PurchaseOrder.PurchaseRequestId, unique GoodsReceipt.IdempotencyKey.
PurchaseRequest has WorkspaceId FK (cascade delete) for visitor isolation.

## Demo workspace / identity (Phase 2)
- Visitor's browser calls POST /api/workspaces once; id stored in localStorage.
- All request reads/writes send X-Demo-Workspace header; backend validates existence and scopes queries.
- Demo identity selected via switcher; sent as X-Demo-User header; backend validates against shared DemoUsers.
- Shared seed catalog (products, suppliers, warehouses, demo users) is read-only and common to all workspaces.
- Limitation: workspace/id headers are not authentication; a determined caller could access another workspace by id. Documented, proportionate for demo.
- No global destructive reset endpoint.

## API endpoints (Phase 2)
- POST /api/workspaces — create workspace
- GET /api/workspaces/{id}
- GET /api/products — shared catalog
- GET /api/demo-users — shared identities
- GET /api/requests — list (scoped)
- GET /api/requests/{id} — detail (scoped)
- POST /api/requests — create (Pending)

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
MSB3277: EF Core 10.0.13 vs Npgsql 10.0.3 transitive EF Core Relational 10.0.4 (no matching standalone version available). Non-blocking; tracked separately.