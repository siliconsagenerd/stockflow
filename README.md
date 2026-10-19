# StockFlow — From Request to Receipt

Beschaffung, die nachvollziehbar bleibt.

A procurement and inventory workflow prototype: request → approval → purchase order →
partial/complete receipt → stock update → history.

## Stack
- Backend: ASP.NET Core / .NET 10, EF Core 10, Npgsql
- Frontend: Vue 3 + TypeScript (Vite)
- Database: PostgreSQL 16
- Tests: xUnit (integration), Playwright (browser, later)

## Local development (oldmac)
Prerequisites: .NET 10 SDK at ~/.dotnet, Node 20+, PostgreSQL reachable.

Database:
- Host: 192.168.3.146 (oldmac), port 5432
- Database: stockflow, user: stockflow
- Set password via env var STOCKFLOW_DB_CONNECTION or appsettings.Development.json (local, not committed)

Backend:
```
cd backend/StockFlow.Api
export STOCKFLOW_DB_CONNECTION="Host=192.168.3.146;Port=5432;Database=stockflow;Username=stockflow;Password=<secret>"
dotnet run
```
API: http://127.0.0.1:5050  (health: /health)

Frontend:
```
cd frontend
npm install
npm run dev
```
Frontend: http://127.0.0.1:5173 (Vite proxies /health and /api to the API)

M2 access via SSH tunnel:
```
ssh -L 5173:127.0.0.1:5173 -L 5050:127.0.0.1:5050 oldmac
```

## Tests
```
cd backend && dotnet test
```

## Status
Phase 1 foundation: schema, seed data, health/connectivity. Workflow features in later phases.

## Limitations
- Demo identities are not authentication.
- No real SAP integration.
- Development database on shared oldmac PostgreSQL instance (isolated by DB/role).