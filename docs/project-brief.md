# StockFlow — Project Brief

## Product
StockFlow — From Request to Receipt
German descriptor: Beschaffung, die nachvollziehbar bleibt.

## Positioning
An independent business-process prototype demonstrating procurement workflow engineering:
request → approval → purchase order → partial/complete receipt → stock update → history.
Not a complete ERP, not a real SAP integration, not an accounting system, not AI.

## Fictional customer problem
A small maintenance supplier manages material requests via spreadsheets and email.
Employees cannot see approval progress; managers lose track of pending requests;
warehouse staff record deliveries separately; partial deliveries make stock and order status inconsistent.

## Target role
dinext AG — Working Student Developer (m/w/d). Transferable skills: business process analysis,
web application design/implementation, OOP, logistics/accounting interest, .NET Core (desirable),
SAP Cloud solutions (conceptual).

## Stack
- Backend: C#, ASP.NET Core, .NET 10 LTS
- Persistence: EF Core 10 + Npgsql 10 (PostgreSQL 16)
- Frontend: Vue 3 + TypeScript (Vite)
- Tests: xUnit (unit/integration), Playwright (browser)
- Packaging: Docker + Compose (Phase 7)
- Hosting: Railway (Phase 7), separate project from SecureDoc

## Non-goals (MVP)
No real SAP integration, no pricing/tax/accounting subsystem, no AI, no enterprise auth,
no admin CRUD beyond walkthrough needs, no ERP simulator (deferred, optional Release 2).

## Demo identities
Employee / Manager / Warehouse operator — labelled "Demo identity — not secure authentication."

## Environment (Phase 1)
- Authoritative repo: oldmac ~/projects/stockflow
- M2: control/edit/browser machine
- API + Vite dev server: oldmac (loopback)
- PostgreSQL: private host (oldmac) container, dedicated DB/role (host not published)
- M2 access via SSH tunnels only