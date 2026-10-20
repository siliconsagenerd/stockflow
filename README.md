# StockFlow — From Request to Receipt

Beschaffung, die nachvollziehbar bleibt.

A procurement and inventory workflow prototype:
request → approval → purchase order → partial/complete receipt → stock update → history.

## Stack

- Backend: ASP.NET Core / .NET 10, EF Core 10, Npgsql
- Frontend: Vue 3 + TypeScript (Vite)
- Database: PostgreSQL 16
- Tests: xUnit (integration, real Postgres), Playwright (browser)

## Local development (oldmac)

Prerequisites: .NET 10 SDK at `~/.dotnet`, Node 20+, PostgreSQL reachable at
`192.168.3.146:5432`.

Database: dedicated `stockflow` (dev) / `stockflow_test` (tests), role `stockflow`.

Secrets live in gitignored files (`.env`, `appsettings.Development.json`).
Use `.env.example` placeholders only.

### Backend

```
cd ~/projects/stockflow/backend/StockFlow.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --urls http://127.0.0.1:5050
```

API: http://127.0.0.1:5050 (health: /health)

### Frontend

```
cd ~/projects/stockflow/frontend
npm install
npm run dev
```

Frontend: http://127.0.0.1:5173 (Vite proxies /api and /health to the API)

### M2 access via SSH tunnel (required; Vite binds to loopback)

```
ssh -f -N -L 5173:127.0.0.1:5173 -L 5050:127.0.0.1:5050 oldmac
```

Then open http://127.0.0.1:5173

## API endpoints

Session / workspace:
- `POST /api/session` — create anonymous session + workspace (sets cookie)
- `GET /api/session` — current session
- `POST /api/workspaces` — alias (creates session)
- `GET /api/workspaces/{id}` — workspace detail (session-scoped)

Catalog:
- `GET /api/products` — shared product catalog
- `GET /api/demo-users` — shared demo identities

Requests:
- `GET /api/requests` — list (workspace-scoped)
- `GET /api/requests/{id}` — detail (workspace-scoped)
- `POST /api/requests` — create Pending request

Approvals:
- `POST /api/requests/{id}/approve` — Manager only, atomic PO creation
- `POST /api/requests/{id}/reject` — Manager only, with reason

Purchase orders:
- `GET /api/purchase-orders` — list (workspace-scoped)
- `GET /api/purchase-orders/{id}` — detail (workspace-scoped)
- `POST /api/purchase-orders/{id}/receive` — Warehouse only, idempotent

Inventory:
- `GET /api/inventory` — workspace-scoped balances
- `GET /api/stock-movements` — workspace-scoped movements

Overview:
- `GET /api/overview` — workspace-scoped metrics + recent activity

## Tests

Set `STOCKFLOW_TEST_DB_CONNECTION` to the stockflow_test connection string
(stored locally, not committed), then:

```
cd backend && dotnet test
```

31 test methods (9 RequestCreation + 10 Approval + 11 Receipt + 1 Health).

Frontend build:

```
cd frontend && npm run build
```

## Status

All phases (1–6) complete. Local acceptance verified.

## Limitations

- Demo identities are simulated, not authentication. Workspace access is
  possession-based via an anonymous cookie.
- No real SAP/ERP integration. No AI, no enterprise auth, no multi-warehouse.
- Opening stock is zero; stock enters only through goods receipts.
- No GitHub/Railway deployment yet.
- MSB3277 warning (EF Core/Npgsql version skew) tracked, not suppressed.

## Troubleshooting

- API unreachable from M2: ensure SSH tunnel is active.
- Test DB: recreate with `DROP DATABASE IF EXISTS stockflow_test; CREATE DATABASE stockflow_test OWNER stockflow;`
- MSB3277 warning: known version skew between EF Core 10.0.13 and Npgsql 10.0.3; not suppressed.

## Live Demo

- **Frontend**: https://stockflow-frontend-mauve.vercel.app
- **API Health**: https://stockflowap1.duckdns.org/health

### What the Demo Does

StockFlow demonstrates a procurement and inventory workflow:
request → approval → purchase order → partial/complete receipt → stock update → history.

Demo identities (Employee, Manager, Warehouse) are **simulated** — not real
authentication. Workspace access is anonymous and possession-based via a cookie.

### Simulated Identities vs Real Authentication

- Demo roles are selected via a dropdown and sent as an  header.
- No passwords, no JWT, no identity provider.
- Anonymous session cookie () grants workspace access.
- Labelled Demo identity — not secure authentication throughout.

### Anonymous Session Limitations

- Sessions are possession-based: anyone with the cookie can access the workspace.
- 8-hour expiry, HttpOnly, SameSite=Strict, Secure in production.
- No user registration, no password recovery, no multi-device sync.

### Zero Opening Stock

- All products start with zero stock.
- Stock enters only through goods receipts against purchase orders.
- No manual stock adjustments or opening balances.

### Known Limitations

- No real SAP/ERP integration.
- No AI, no enterprise auth, no multi-warehouse, no transfers.
- MSB3277 warning (EF Core/Npgsql version skew, tracked, not suppressed).
- Supabase free tier (7-day backup retention, 500MB limit).
- Oracle Cloud free tier (short-lived deployment).

### Deployment Status

Deployed 2026-10-20. Frontend on Vercel, API on Oracle Cloud, DB on Supabase.
See  for operational details.


## Live Demo

- **Frontend**: https://stockflow-frontend-mauve.vercel.app
- **API Health**: https://stockflowap1.duckdns.org/health

### What the Demo Does

StockFlow demonstrates a procurement and inventory workflow:
request to approval to purchase order to partial/complete receipt to stock update to history.

Demo identities (Employee, Manager, Warehouse) are simulated, not real
authentication. Workspace access is anonymous and possession-based via a cookie.

### Simulated Identities vs Real Authentication

- Demo roles are selected via a dropdown and sent as an X-Demo-User header.
- No passwords, no JWT, no identity provider.
- Anonymous session cookie grants workspace access.
- Labelled Demo identity - not secure authentication throughout.

### Anonymous Session Limitations

- Sessions are possession-based: anyone with the cookie can access the workspace.
- 8-hour expiry, HttpOnly, SameSite=Strict, Secure in production.
- No user registration, no password recovery, no multi-device sync.

### Zero Opening Stock

- All products start with zero stock.
- Stock enters only through goods receipts against purchase orders.
- No manual stock adjustments or opening balances.

### Known Limitations

- No real SAP/ERP integration.
- No AI, no enterprise auth, no multi-warehouse, no transfers.
- MSB3277 warning (EF Core/Npgsql version skew, tracked, not suppressed).
- Supabase free tier (7-day backup retention, 500MB limit).
- Oracle Cloud free tier (short-lived deployment).

### Deployment Status

Deployed 2026-10-20. Frontend on Vercel, API on Oracle Cloud, DB on Supabase.
See docs/RUNBOOK.md for operational details.
