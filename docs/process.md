# Process — StockFlow

## Fictional customer problem
A small maintenance supplier manages material requests through spreadsheets and email.
Employees cannot see approval progress; managers lose track of pending requests;
warehouse staff record deliveries separately; partial deliveries make stock and order status inconsistent.

## As-is (spreadsheet/email)
1. Employee emails a request for materials.
2. Manager approves/rejects by reply (often lost).
3. Someone manually creates a purchase order.
4. Warehouse records delivery in a separate sheet.
5. Stock and outstanding quantities drift apart.

## To-be (StockFlow)
Employee requests materials → Manager approves/rejects → approval creates one purchase order →
warehouse records partial or complete receipt → stock and outstanding quantities update →
history explains every transition.

## Core demo
Request 10 → approve → receive 6 → stock +6, order partially received → receive 4 → stock +4, order completed.

## Assumptions
- One warehouse initially.
- One product per request, positive integer quantity.
- Each product has one seeded preferred supplier.
- Whole units only; no pricing/tax/accounting.
- Synthetic data only.

## Excluded scope (MVP)
Real SAP integration, pricing/tax, multi-warehouse, admin CRUD, ERP simulator (optional Release 2).