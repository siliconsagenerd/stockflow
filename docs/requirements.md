# Requirements — StockFlow

## User stories

### Phase 1 (foundation)
1. As a visitor, I can open the app and see whether the API/database are reachable.
2. As a developer, I can run the API and have the schema migrated + seeded automatically in dev.
3. As a developer, I can build and test the backend and frontend.

### Phase 2 (request creation)
4. As an employee (demo identity), I can select a product and request a positive whole-number quantity with an optional justification.
5. As a visitor, each browser session gets an isolated demo workspace; requests are scoped to it.
6. As a visitor, I can switch the simulated demo identity (Employee/Manager/Warehouse).
7. As a visitor, I can see the list of requests in my workspace and a success/error message.

## Acceptance criteria (Phase 2)
- POST /api/workspaces creates a server-scoped workspace; id stored client-side.
- GET /api/products and /api/demo-users return shared seed catalog.
- POST /api/requests validates product, quantity (>0 int), justification (<=500 chars), workspace, and demo identity.
- Invalid submissions return 400 with a clear error and create no record.
- GET /api/requests and GET /api/requests/{id} are scoped to the workspace; cross-workspace access returns 404.
- Frontend submits via real API and displays persisted request; validation errors shown.
- Loading, empty, validation, success, and error states present.

## Tests (Phase 2)
- 7 integration tests against real PostgreSQL (stockflow_test): valid persist, invalid quantities, unknown product/invalid context, overlong justification, scoped list/detail, cross-workspace 404.
- 1 health integration test (Phase 1).
- Browser: submission displays persisted request; validation error does not fabricate success.

## Later slices
- Phase 3: approval + atomic PO creation
- Phase 4: goods receipt, stock, idempotency, concurrency
- Phase 5: overview dashboard, lifecycle timeline, UX polish