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
### Phase 3 (approval + atomic PO creation)
8. As a Manager (demo identity), I can view Pending requests in my workspace and approve or reject them.
9. As a Manager, approval atomically creates exactly one Purchase Order (Open) derived from trusted stored records.
10. As a Manager, I can reject a Pending request with a bounded reason; rejected requests cannot later be approved.
11. As any identity, I can view Purchase Orders scoped to my workspace.
## Acceptance criteria (Phase 3)
- POST /api/requests/{id}/approve requires a valid session + Manager demo identity; self-approval rejected (403); non-Manager rejected (403); only Pending can transition (409 otherwise).
- Approval atomically updates request to Approved (with decidedBy + decidedAt) and creates one PurchaseOrder (status Open) with supplier/warehouse/product/quantity derived from stored records.
- Repeated approval returns 409 and creates no duplicate order (unique constraint on PurchaseOrder.PurchaseRequestId).
- Concurrent approvals yield exactly one order.
- POST /api/requests/{id}/reject requires Manager; records decidedBy, decidedAt, bounded reason (<=500 chars); rejected cannot be approved.
- Concurrent approve/reject yields one consistent final state.
- GET /api/purchase-orders and GET /api/purchase-orders/{id} scoped to session workspace; cross-workspace returns 404.
- Transaction alone is NOT sufficient for concurrent safety; the conditional status transition (ExecuteUpdate with WHERE status='Pending') plus the unique constraint on PurchaseOrder.PurchaseRequestId are the actual guards.

### Phase 4 (goods receipt, stock, idempotency, concurrency)
12. As a Warehouse (demo identity), I can receive goods against an eligible PO with a positive whole-number quantity.
13. As a visitor, I can see workspace-specific stock balances and movement history.
## Acceptance criteria (Phase 4)
- POST /api/purchase-orders/{id}/receive requires valid session + Warehouse identity; order must belong to workspace and be eligible (Open/PartiallyReceived).
- Quantity must be positive integer <= outstanding; over-receipt and completed-order receipts rejected.
- Receipt atomically creates GoodsReceipt, updates order received/status, updates StockBalance, creates StockMovement. Rollback on any failure.
- Idempotency: same (workspace, key) + same payload returns original; conflicting payload rejected; concurrent duplicates produce one receipt.
- Concurrency: conditional transitions + Serializable isolation; bounded 409 on conflict.
- StockBalance unique (WorkspaceId, ProductId, WarehouseId); zero opening balances (no seed stock movements).
- GET /api/inventory and GET /api/stock-movements scoped to session workspace.
