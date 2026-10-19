# StockFlow — From Request to Receipt

Beschaffung, die nachvollziehbar bleibt.

A procurement and inventory workflow prototype: request → approval → purchase order →
partial/complete receipt → stock update → history.

## Stack
- Backend: ASP.NET Core / .NET 10, EF Core 10, Npgsql
- Frontend: Vue 3 + TypeScript (Vite)
- Database: PostgreSQL 16
- Tests: xUnit (integration, real Postgres), Playwright (browser)

## Local development (oldmac)
Prerequisites: .NET 10 SDK at ~/.dotnet, Node 20+, PostgreSQL reachable at 192.168.3.146:5432.

Database: dedicated `stockflow` (dev) / `stockflow_test` (tests), role `stockflow`.

Backend:
```
cd ~/projects/stockflow/backend/StockFlow.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5050
```
API: http://127.0.0.1:5050  (health: /health)

Frontend:
```
cd ~/projects/stockflow/frontend
npm install
npm run dev
```
Frontend: http://127.0.0.1:5173 (Vite proxies /api and /health to the API)

M2 access via SSH tunnel (required; Vite binds to loopback):
```
ssh -f -N -L 5173:127.0.0.1:5173 -L 5050:127.0.0.1:5050 oldmac
```
Then open http://127.0.0.1:5173

## API endpoints (Phase 2)
- POST /api/session — create anonymous session + workspace (sets cookie)
- GET /api/session — current session
- POST /api/workspaces — alias (creates session)
- GET /api/workspaces/{id} — workspace detail (session-scoped)
- GET /api/products — shared catalog
- GET /api/demo-users — shared identities
- GET /api/requests — list (workspace-scoped)
- GET /api/requests/{id} — detail (workspace-scoped)
- POST /api/requests — create Pending request

## Tests
Set `STOCKFLOW_TEST_DB_CONNECTION` to the stockflow_test connection string
(stored locally, not committed), then:
```
cd backend && dotnet test
```

## Status
Phase 2: request creation vertical slice complete. Workflow features (approval, orders, receipts) in later phases.

## Limitations
- Demo identities are not authentication; workspace headers are not authorization.
- No real SAP integration.
- Shared postgres instance for dev (isolated by DB/role); production uses Railway's own Postgres (Phase 7).
- MSB3277 warning (EF Core / Npgsql version skew) tracked separately.
