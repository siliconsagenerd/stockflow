using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly StockFlowDbContext _db;

    public PurchaseOrdersController(StockFlowDbContext db) => _db = db;

    private async Task<(DemoSession? Session, IActionResult? Error)> ResolveSessionAsync()
    {
        var session = await SessionContext.ResolveAsync(_db, HttpContext);
        if (session is null) return (null, Unauthorized(new { error = "No valid session." }));
        return (session, null);
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        var items = await _db.PurchaseOrders
            .Where(o => o.PurchaseRequest.WorkspaceId == wsId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new PurchaseOrderResponseDto(
                o.Id, o.Reference, o.PurchaseRequestId, o.SupplierId, o.Supplier.Name,
                o.WarehouseId, o.Warehouse.Name, o.PurchaseRequest.ProductId, o.PurchaseRequest.Product.Name,
                o.PurchaseRequest.Product.Sku, o.OrderedQuantity, o.ReceivedQuantity, o.Status, o.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        var o = await _db.PurchaseOrders
            .Where(x => x.Id == id && x.PurchaseRequest.WorkspaceId == wsId)
            .Select(x => new PurchaseOrderResponseDto(
                x.Id, x.Reference, x.PurchaseRequestId, x.SupplierId, x.Supplier.Name,
                x.WarehouseId, x.Warehouse.Name, x.PurchaseRequest.ProductId, x.PurchaseRequest.Product.Name,
                x.PurchaseRequest.Product.Sku, x.OrderedQuantity, x.ReceivedQuantity, x.Status, x.CreatedAt))
            .FirstOrDefaultAsync();

        if (o is null) return NotFound(new { error = "Purchase order not found." });
        return Ok(o);
    }

    [HttpGet("{id:guid}/receipts")]
    public async Task<IActionResult> GetReceipts(Guid id)
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        var exists = await _db.PurchaseOrders
            .AnyAsync(x => x.Id == id && x.PurchaseRequest.WorkspaceId == wsId);
        if (!exists) return NotFound(new { error = "Purchase order not found." });

        var list = await _db.GoodsReceipts
            .Where(g => g.PurchaseOrderId == id && g.WorkspaceId == wsId)
            .OrderBy(g => g.ReceivedAt)
            .Select(g => new ReceiptResponseDto(
                g.Id, g.PurchaseOrderId, g.WorkspaceId, g.Quantity,
                g.ReceivedById, g.ReceivedBy.DisplayName, g.ReceivedAt, g.IdempotencyKey,
                g.PurchaseOrder.ReceivedQuantity, g.PurchaseOrder.Status))
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost("{id:guid}/receive")]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceiveRequestDto dto)
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        DemoUser receiver;
        try { receiver = await DemoContext.ResolveUserAsync(_db, HttpContext); }
        catch (DemoContextException e) { return BadRequest(new { error = e.Message }); }

        if (receiver.Role != "Warehouse")
            return StatusCode(403, new { error = "Only a Warehouse demo identity can receive goods." });

        if (dto.Quantity < 1)
            return BadRequest(new { error = "Quantity must be a positive whole number." });

        if (string.IsNullOrWhiteSpace(dto.IdempotencyKey))
            return BadRequest(new { error = "Idempotency key is required." });

        var key = dto.IdempotencyKey.Trim();

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Idempotency lookup (scoped to workspace).
            var existing = await _db.GoodsReceipts
                .Include(g => g.ReceivedBy)
                .FirstOrDefaultAsync(g => g.WorkspaceId == wsId && g.IdempotencyKey == key);

            if (existing is not null)
            {
                if (existing.PurchaseOrderId != id || existing.Quantity != dto.Quantity)
                {
                    await tx.RollbackAsync();
                    return Conflict(new { error = "Idempotency key conflict: different order or quantity." });
                }
                // Return original result.
                var origOrder = await _db.PurchaseOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);
                await tx.CommitAsync();
                return Ok(new ReceiptResponseDto(
                    existing.Id, existing.PurchaseOrderId, existing.WorkspaceId, existing.Quantity,
                    existing.ReceivedById, existing.ReceivedBy.DisplayName, existing.ReceivedAt, existing.IdempotencyKey,
                    origOrder?.ReceivedQuantity ?? existing.Quantity, origOrder?.Status ?? "Completed"));
            }

            var order = await _db.PurchaseOrders
                .Include(o => o.PurchaseRequest)
                .FirstOrDefaultAsync(o => o.Id == id && o.PurchaseRequest.WorkspaceId == wsId);

            if (order is null)
            {
                await tx.RollbackAsync();
                return NotFound(new { error = "Purchase order not found." });
            }

            if (order.Status == "Completed")
            {
                await tx.RollbackAsync();
                return Conflict(new { error = "Order is already completed." });
            }

            var outstanding = order.OrderedQuantity - order.ReceivedQuantity;
            if (dto.Quantity > outstanding)
            {
                await tx.RollbackAsync();
                return BadRequest(new { error = $"Quantity exceeds outstanding ({outstanding})." });
            }

            var productId = order.PurchaseRequest.ProductId;

            // Load or create stock balance.
            var balance = await _db.StockBalances
                .FirstOrDefaultAsync(b => b.WorkspaceId == wsId && b.ProductId == productId && b.WarehouseId == order.WarehouseId);
            if (balance is null)
            {
                balance = new StockBalance
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = wsId,
                    ProductId = productId,
                    WarehouseId = order.WarehouseId,
                    Quantity = 0
                };
                _db.StockBalances.Add(balance);
            }

            var receipt = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                WorkspaceId = wsId,
                PurchaseOrderId = order.Id,
                Quantity = dto.Quantity,
                ReceivedById = receiver.Id,
                ReceivedAt = DateTime.UtcNow,
                IdempotencyKey = key
            };
            _db.GoodsReceipts.Add(receipt);

            order.ReceivedQuantity += dto.Quantity;
            order.Status = order.ReceivedQuantity >= order.OrderedQuantity ? "Completed" : "PartiallyReceived";

            balance.Quantity += dto.Quantity;

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                WorkspaceId = wsId,
                ProductId = productId,
                WarehouseId = order.WarehouseId,
                Delta = dto.Quantity,
                Reason = "GoodsReceipt",
                ReferenceId = receipt.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.StockMovements.Add(movement);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            var resp = new ReceiptResponseDto(
                receipt.Id, receipt.PurchaseOrderId, receipt.WorkspaceId, receipt.Quantity,
                receipt.ReceivedById, receiver.DisplayName, receipt.ReceivedAt, receipt.IdempotencyKey,
                order.ReceivedQuantity, order.Status);

            return CreatedAtAction(nameof(Get), new { id = order.Id }, resp);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync();
            // Concurrent duplicate submission: look up the existing receipt.
            var existing = await _db.GoodsReceipts
                .Include(g => g.ReceivedBy)
                .FirstOrDefaultAsync(g => g.WorkspaceId == wsId && g.IdempotencyKey == key);
            if (existing is not null && existing.PurchaseOrderId == id && existing.Quantity == dto.Quantity)
            {
                var order = await _db.PurchaseOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                return Ok(new ReceiptResponseDto(
                    existing.Id, existing.PurchaseOrderId, existing.WorkspaceId, existing.Quantity,
                    existing.ReceivedById, existing.ReceivedBy.DisplayName, existing.ReceivedAt, existing.IdempotencyKey,
                    order?.ReceivedQuantity ?? existing.Quantity, order?.Status ?? "Completed"));
            }
            return Conflict(new { error = "Concurrent conflict or idempotency key conflict." });
        }
        catch (Exception ex) when (IsSerializationFailure(ex))
        {
            await tx.RollbackAsync();
            return Conflict(new { error = "Concurrent conflict; retry." });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message?.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true
            || ex.InnerException?.Message?.Contains("23505", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsSerializationFailure(Exception ex)
    {
        return ex is DbUpdateException due && due.InnerException?.Message?.Contains("40001", StringComparison.OrdinalIgnoreCase) == true
            || ex.Message.Contains("40001", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("serialization", StringComparison.OrdinalIgnoreCase);
    }
}
