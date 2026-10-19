# Requirements — StockFlow (Phase 1 foundation)

## User stories (Phase 1)
1. As a visitor, I can open the app and see whether the API/database are reachable.
2. As a developer, I can run the API and have the schema migrated + seeded automatically in dev.
3. As a developer, I can build and test the backend and frontend.

## Acceptance criteria (Phase 1)
- `dotnet build` succeeds.
- `dotnet test` runs at least one meaningful test (health/connectivity).
- `npm run build` succeeds.
- `/health` returns 200 with `database: connected` when Postgres is reachable.
- Schema contains Product, Supplier, Warehouse, DemoUser, PurchaseRequest, PurchaseOrder, GoodsReceipt, StockMovement.
- Seed data: 2 suppliers, 5 products, 1 warehouse, 3 demo users, initial stock movements matching balances.
- Frontend renders at desktop and narrow widths without overflow.

## Later slices
- Phase 2: request creation
- Phase 3: approval + atomic PO creation
- Phase 4: goods receipt, stock, idempotency, concurrency
- Phase 5: overview dashboard, lifecycle timeline, UX polish