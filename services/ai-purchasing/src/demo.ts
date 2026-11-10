import { PGlite } from "@electric-sql/pglite";
import type { Queryable } from "./types.js";

const schema = `
CREATE TABLE "Products" (
  "Id" uuid PRIMARY KEY,
  "Sku" text NOT NULL,
  "Name" text NOT NULL,
  "Unit" text NOT NULL DEFAULT 'pcs',
  "PreferredSupplierId" uuid,
  "CurrentStock" integer NOT NULL DEFAULT 0
);
CREATE TABLE "Suppliers" ("Id" uuid PRIMARY KEY, "Name" text NOT NULL, "ContactEmail" text NOT NULL);
CREATE TABLE "Warehouses" ("Id" uuid PRIMARY KEY, "Name" text NOT NULL, "Location" text NOT NULL);
CREATE TABLE "DemoWorkspaces" ("Id" uuid PRIMARY KEY, "CreatedAt" timestamptz NOT NULL, "LastAccessedAt" timestamptz NOT NULL);
CREATE TABLE "DemoUsers" ("Id" uuid PRIMARY KEY, "DisplayName" text NOT NULL, "Role" text NOT NULL);
CREATE TABLE "PurchaseRequests" (
  "Id" uuid PRIMARY KEY,
  "WorkspaceId" uuid NOT NULL,
  "ProductId" uuid NOT NULL,
  "Quantity" integer NOT NULL,
  "Justification" text,
  "RequesterId" uuid NOT NULL,
  "Status" text NOT NULL DEFAULT 'Pending',
  "CreatedAt" timestamptz NOT NULL,
  "DecidedAt" timestamptz,
  "DecidedById" uuid,
  "RejectionReason" text
);
CREATE TABLE "PurchaseOrders" (
  "Id" uuid PRIMARY KEY,
  "Reference" text NOT NULL,
  "PurchaseRequestId" uuid NOT NULL,
  "SupplierId" uuid NOT NULL,
  "WarehouseId" uuid NOT NULL,
  "OrderedQuantity" integer NOT NULL,
  "ReceivedQuantity" integer NOT NULL DEFAULT 0,
  "Status" text NOT NULL DEFAULT 'Open',
  "CreatedAt" timestamptz NOT NULL
);
CREATE TABLE "StockBalances" (
  "Id" uuid PRIMARY KEY,
  "WorkspaceId" uuid NOT NULL,
  "ProductId" uuid NOT NULL,
  "WarehouseId" uuid NOT NULL,
  "Quantity" integer NOT NULL
);
`;

const ws = "11111111-1111-4111-8111-111111111111";
const users = {
  anna: "22222222-2222-4222-8222-222222222222",
  mehmet: "33333333-3333-4333-8333-333333333333",
  lena: "44444444-4444-4444-8444-444444444444",
};
const suppliers = {
  nordic: "55555555-5555-4555-8555-555555555555",
  rhein: "66666666-6666-4666-8666-666666666666",
};
const warehouse = "77777777-7777-4777-8777-777777777777";

const products = [
  { id: "a0000000-0000-4000-8000-000000000001", sku: "FLT-0140", name: "Filterpatrone 140 mm", unit: "Stk", stock: 0, supplier: suppliers.nordic },
  { id: "a0000000-0000-4000-8000-000000000002", sku: "VLV-0025", name: "Absperrventil DN25", unit: "Stk", stock: 11, supplier: suppliers.rhein },
  { id: "a0000000-0000-4000-8000-000000000003", sku: "SNR-0090", name: "Drucksensor 0-10 bar", unit: "Stk", stock: 48, supplier: suppliers.nordic },
  { id: "a0000000-0000-4000-8000-000000000004", sku: "GSK-0310", name: "Dichtungssatz Pumpe P3", unit: "Set", stock: 4, supplier: suppliers.rhein },
];

const seedRequests = [
  { id: "b0000000-0000-4000-8000-000000000001", product: 0, qty: 40, user: users.anna, just: "Linie 3 steht, Filter sind seit Freitag leer und der naechste Service ist Donnerstag.", ageDays: 1, status: "Pending" },
  { id: "b0000000-0000-4000-8000-000000000002", product: 1, qty: 60, user: users.mehmet, just: "Reserve", ageDays: 2, status: "Pending" },
  { id: "b0000000-0000-4000-8000-000000000003", product: 2, qty: 6, user: users.lena, just: "Ersatz fuer zwei defekte Sensoren an der Abfuellanlage, Austausch am Montag geplant.", ageDays: 3, status: "Pending" },
  { id: "b0000000-0000-4000-8000-000000000004", product: 3, qty: 2, user: users.anna, just: "Pumpenwartung Q4, Dichtungen sind verschlissen.", ageDays: 4, status: "Pending" },
];

const seedHistory = [
  { id: "c0000000-0000-4000-8000-000000000001", product: 0, qty: 40, status: "Approved", days: 20 },
  { id: "c0000000-0000-4000-8000-000000000002", product: 0, qty: 45, status: "Approved", days: 55 },
  { id: "c0000000-0000-4000-8000-000000000003", product: 0, qty: 38, status: "Approved", days: 80 },
  { id: "c0000000-0000-4000-8000-000000000004", product: 1, qty: 12, status: "Approved", days: 18 },
  { id: "c0000000-0000-4000-8000-000000000005", product: 1, qty: 15, status: "Rejected", days: 40 },
  { id: "c0000000-0000-4000-8000-000000000006", product: 1, qty: 10, status: "Rejected", days: 62 },
  { id: "c0000000-0000-4000-8000-000000000007", product: 1, qty: 11, status: "Rejected", days: 77 },
  { id: "c0000000-0000-4000-8000-000000000008", product: 2, qty: 8, status: "Approved", days: 25 },
  { id: "c0000000-0000-4000-8000-000000000009", product: 2, qty: 6, status: "Approved", days: 70 },
  { id: "c0000000-0000-4000-8000-000000000010", product: 3, qty: 3, status: "Approved", days: 30 },
];

export async function createDemoDatabase(): Promise<{ db: Queryable; close: () => Promise<void> }> {
  const lite = await PGlite.create();
  await lite.exec(schema);

  await lite.query('INSERT INTO "DemoWorkspaces" VALUES ($1, now(), now())', [ws]);
  await lite.query('INSERT INTO "Suppliers" VALUES ($1, $2, $3), ($4, $5, $6)', [
    suppliers.nordic, "Nordic Industrial Supply", "orders@nordic.example",
    suppliers.rhein, "Rhein Filtertechnik", "vertrieb@rheinfilter.example",
  ]);
  await lite.query('INSERT INTO "Warehouses" VALUES ($1, $2, $3)', [
    warehouse, "Hauptlager Saarbruecken", "Saarbruecken, DE",
  ]);
  await lite.query('INSERT INTO "DemoUsers" VALUES ($1, $2, $3), ($4, $5, $6), ($7, $8, $9)', [
    users.anna, "Anna Beispiel", "Employee",
    users.mehmet, "Mehmet Yilmaz", "Manager",
    users.lena, "Lena Krause", "Employee",
  ]);

  for (const product of products) {
    await lite.query(
      'INSERT INTO "Products" ("Id","Sku","Name","Unit","PreferredSupplierId","CurrentStock") VALUES ($1,$2,$3,$4,$5,$6)',
      [product.id, product.sku, product.name, product.unit, product.supplier, product.stock],
    );
    await lite.query(
      'INSERT INTO "StockBalances" ("Id","WorkspaceId","ProductId","WarehouseId","Quantity") VALUES (gen_random_uuid(),$1,$2,$3,$4)',
      [ws, product.id, warehouse, product.stock],
    );
  }

  for (const request of seedRequests) {
    await lite.query(
      'INSERT INTO "PurchaseRequests" ("Id","WorkspaceId","ProductId","Quantity","Justification","RequesterId","Status","CreatedAt") VALUES ($1,$2,$3,$4,$5,$6,$7, now() - ($8 || \' days\')::interval)',
      [request.id, ws, products[request.product]!.id, request.qty, request.just, request.user, request.status, String(request.ageDays)],
    );
  }

  for (const entry of seedHistory) {
    await lite.query(
      'INSERT INTO "PurchaseRequests" ("Id","WorkspaceId","ProductId","Quantity","Justification","RequesterId","Status","CreatedAt","DecidedAt") VALUES ($1,$2,$3,$4,$5,$6,$7, now() - ($8 || \' days\')::interval, now() - ($8 || \' days\')::interval)',
      [entry.id, ws, products[entry.product]!.id, entry.qty, "Historischer Vorgang", users.anna, entry.status, String(entry.days)],
    );
  }

  return {
    db: lite as unknown as Queryable,
    close: async () => {
      await lite.close();
    },
  };
}
