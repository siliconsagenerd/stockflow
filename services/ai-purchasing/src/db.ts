import type { PendingRequest, ProductHistory, Queryable } from "./types.js";

export async function listWorkspaces(db: Queryable) {
  const { rows } = await db.query<{ id: string; lastAccessedAt: string }>(
    'SELECT "Id" AS id, "LastAccessedAt" AS "lastAccessedAt" FROM "DemoWorkspaces" ORDER BY "LastAccessedAt" DESC LIMIT 20',
  );
  return rows;
}

export async function listPendingRequests(
  db: Queryable,
  workspaceId: string | null,
): Promise<PendingRequest[]> {
  const { rows } = await db.query<PendingRequest>(
    `SELECT r."Id" AS id,
            r."WorkspaceId" AS "workspaceId",
            r."ProductId" AS "productId",
            p."Name" AS "productName",
            p."Sku" AS sku,
            p."Unit" AS unit,
            r."Quantity" AS quantity,
            r."Justification" AS justification,
            u."DisplayName" AS requester,
            r."CreatedAt" AS "createdAt",
            p."CurrentStock" AS "currentStock"
       FROM "PurchaseRequests" r
       JOIN "Products" p ON p."Id" = r."ProductId"
       JOIN "DemoUsers" u ON u."Id" = r."RequesterId"
      WHERE r."Status" = 'Pending'
        AND ($1::uuid IS NULL OR r."WorkspaceId" = $1::uuid)
      ORDER BY r."CreatedAt" ASC`,
    [workspaceId],
  );
  return rows;
}

export async function loadHistory(
  db: Queryable,
  productIds: string[],
): Promise<Map<string, ProductHistory>> {
  const map = new Map<string, ProductHistory>();
  if (productIds.length === 0) return map;
  const wanted = new Set(productIds);

  const { rows } = await db.query<{
    productId: string;
    sampleSize: number;
    averageQuantity: number;
    rejectedCount: number;
  }>(
    `SELECT "ProductId" AS "productId",
            COUNT(*)::int AS "sampleSize",
            COALESCE(AVG("Quantity"), 0)::float AS "averageQuantity",
            SUM(CASE WHEN "Status" = 'Rejected' THEN 1 ELSE 0 END)::int AS "rejectedCount"
       FROM "PurchaseRequests"
      WHERE "Status" <> 'Pending'
        AND "CreatedAt" > now() - interval '90 days'
      GROUP BY "ProductId"`,
  );

  for (const row of rows) {
    if (!wanted.has(row.productId)) continue;
    map.set(row.productId, {
      productId: row.productId,
      sampleSize: Number(row.sampleSize),
      averageQuantity: Number(row.averageQuantity),
      rejectedCount: Number(row.rejectedCount),
    });
  }
  return map;
}

export async function countPendingDuplicates(
  db: Queryable,
  requests: PendingRequest[],
): Promise<Map<string, number>> {
  const counts = new Map<string, number>();
  for (const request of requests) {
    counts.set(request.productId, (counts.get(request.productId) ?? 0) + 1);
  }
  const duplicates = new Map<string, number>();
  for (const [productId, count] of counts) {
    duplicates.set(productId, Math.max(0, count - 1));
  }
  return duplicates;
}
