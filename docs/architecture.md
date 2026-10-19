# Architecture — StockFlow

## Components
- ASP.NET Core Web API (.NET 10) — controllers, EF Core DbContext, seed, health.
- PostgreSQL 16 — schema + data.
- Vue 3 + TS (Vite) — SPA, proxies /health and /api to API.

## Data model (Phase 1)
Product, Supplier, Warehouse, DemoUser, PurchaseRequest, PurchaseOrder, GoodsReceipt, StockMovement.
Constraints: unique Sku, unique PurchaseOrder.PurchaseRequestId, unique GoodsReceipt.IdempotencyKey.

## Transaction boundaries (later phases)
- Approval + PO creation: single transaction.
- Receipt + stock update + movement: single transaction.
- Idempotency: repeated same key+payload returns existing outcome; conflicting payload rejected.

## Concurrency (documented, implemented in Phase 4)
Row-level locking / unique constraints + transaction isolation (Serializable or optimistic with retry).

## Health
`GET /health` combines process liveness and database readiness (CanConnect).
Returns 200 Healthy / 503 Unhealthy with JSON body. Future: split /health (process) and /ready (db).

## Demo identity / isolation
Demo user switcher (Employee/Manager/Warehouse) is simulated identity, not auth.
Workspace isolation approach TBD in Phase 2 (server-scoped demo workspace). No cross-workspace access by ID.

## Possible future ERP boundary (optional Release 2, deferred)
```
IErpPurchaseOrderGateway
  SubmitPurchaseOrder(...)
  GetSubmissionStatus(...)
```
Simulator only, explicitly labelled. Not implemented in MVP.

## Dev networking
- API: oldmac 127.0.0.1:5050
- Vite: oldmac 0.0.0.0:5173
- Postgres: oldmac 192.168.3.146:5432 (existing container, dedicated DB/role)
- M2 access via SSH tunnels; no public exposure.