using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;
using StockFlow.Api.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/requests")]
public class RequestsController : ControllerBase
{
    private readonly StockFlowDbContext _db;
    private const int MaxJustification = 500;
    private const int MaxRejectionReason = 500;

    public RequestsController(StockFlowDbContext db) => _db = db;

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

        var items = await _db.PurchaseRequests
            .Where(r => r.WorkspaceId == wsId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RequestResponseDto(
                r.Id, r.ProductId, r.Product.Name, r.Product.Sku, r.Product.Unit,
                r.Quantity, r.Justification, r.Status, r.RequesterId, r.Requester.DisplayName,
                r.CreatedAt, r.WorkspaceId))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        var r = await _db.PurchaseRequests
            .Where(x => x.Id == id && x.WorkspaceId == wsId)
            .Select(x => new RequestResponseDto(
                x.Id, x.ProductId, x.Product.Name, x.Product.Sku, x.Product.Unit,
                x.Quantity, x.Justification, x.Status, x.RequesterId, x.Requester.DisplayName,
                x.CreatedAt, x.WorkspaceId))
            .FirstOrDefaultAsync();

        if (r is null) return NotFound(new { error = "Request not found." });
        return Ok(r);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto dto)
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        DemoUser requester;
        try { requester = await DemoContext.ResolveUserAsync(_db, HttpContext); }
        catch (DemoContextException e) { return BadRequest(new { error = e.Message }); }

        if (dto.Quantity < 1)
            return BadRequest(new { error = "Quantity must be a positive whole number." });

        if (dto.Justification is not null && dto.Justification.Length > MaxJustification)
            return BadRequest(new { error = $"Justification must be at most {MaxJustification} characters." });

        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product is null) return BadRequest(new { error = "Unknown product." });

        var entity = new PurchaseRequest
        {
            Id = Guid.NewGuid(),
            WorkspaceId = wsId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Justification = string.IsNullOrWhiteSpace(dto.Justification) ? null : dto.Justification.Trim(),
            RequesterId = requester.Id,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _db.PurchaseRequests.Add(entity);
        await _db.SaveChangesAsync();

        var response = new RequestResponseDto(
            entity.Id, product.Id, product.Name, product.Sku, product.Unit,
            entity.Quantity, entity.Justification, entity.Status, requester.Id, requester.DisplayName,
            entity.CreatedAt, wsId);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, response);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        DemoUser decider;
        try { decider = await DemoContext.ResolveUserAsync(_db, HttpContext); }
        catch (DemoContextException e) { return BadRequest(new { error = e.Message }); }

        if (decider.Role != "Manager")
            return StatusCode(403, new { error = "Only a Manager demo identity can approve." });

        // Load trusted stored data first.
        var request = await _db.PurchaseRequests
            .AsNoTracking()
            .Include(r => r.Product)
            .FirstOrDefaultAsync(r => r.Id == id && r.WorkspaceId == wsId);
        if (request is null) return NotFound(new { error = "Request not found." });
        if (request.RequesterId == decider.Id)
            return StatusCode(403, new { error = "Self-approval is not allowed." });

        var product = request.Product;
        var supplierId = product.PreferredSupplierId ?? await _db.Suppliers.Select(s => s.Id).FirstOrDefaultAsync();
        var warehouseId = await _db.Warehouses.Select(w => w.Id).FirstOrDefaultAsync();
        if (supplierId == Guid.Empty || warehouseId == Guid.Empty)
            return StatusCode(500, new { error = "No supplier/warehouse configured." });

        var decidedAt = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Conditional transition: only Pending -> Approved. Guards against concurrent approve/reject.
            var rows = await _db.PurchaseRequests
                .Where(r => r.Id == id && r.WorkspaceId == wsId && r.Status == "Pending")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Approved")
                    .SetProperty(r => r.DecidedById, decider.Id)
                    .SetProperty(r => r.DecidedAt, decidedAt));

            if (rows == 0)
            {
                await tx.RollbackAsync();
                return Conflict(new { error = "Request is no longer Pending." });
            }

            var po = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                Reference = "PO-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                PurchaseRequestId = request.Id,
                SupplierId = supplierId,
                WarehouseId = warehouseId,
                OrderedQuantity = request.Quantity,
                ReceivedQuantity = 0,
                Status = "Open",
                CreatedAt = DateTime.UtcNow
            };
            _db.PurchaseOrders.Add(po);

            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync();
                return Conflict(new { error = "Order already exists for this request." });
            }

            await tx.CommitAsync();

            var supplier = await _db.Suppliers.FindAsync(po.SupplierId);
            var warehouse = await _db.Warehouses.FindAsync(po.WarehouseId);

            var response = new PurchaseOrderResponseDto(
                po.Id, po.Reference, po.PurchaseRequestId, po.SupplierId, supplier!.Name,
                po.WarehouseId, warehouse!.Name, product.Id, product.Name, product.Sku,
                po.OrderedQuantity, po.ReceivedQuantity, po.Status, po.CreatedAt);

            return CreatedAtAction("Get", "PurchaseOrders", new { id = po.Id }, response);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequestDto dto)
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        DemoUser decider;
        try { decider = await DemoContext.ResolveUserAsync(_db, HttpContext); }
        catch (DemoContextException e) { return BadRequest(new { error = e.Message }); }

        if (decider.Role != "Manager")
            return StatusCode(403, new { error = "Only a Manager demo identity can reject." });

        var reason = dto?.Reason?.Trim();
        if (!string.IsNullOrEmpty(reason) && reason.Length > MaxRejectionReason)
            return BadRequest(new { error = $"Rejection reason must be at most {MaxRejectionReason} characters." });

        var request = await _db.PurchaseRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.WorkspaceId == wsId);
        if (request is null) return NotFound(new { error = "Request not found." });
        if (request.RequesterId == decider.Id)
            return StatusCode(403, new { error = "Self-rejection is not allowed." });

        var decidedAt = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var rows = await _db.PurchaseRequests
                .Where(r => r.Id == id && r.WorkspaceId == wsId && r.Status == "Pending")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, "Rejected")
                    .SetProperty(r => r.DecidedById, decider.Id)
                    .SetProperty(r => r.DecidedAt, decidedAt)
                    .SetProperty(r => r.RejectionReason, string.IsNullOrEmpty(reason) ? null : reason));

            if (rows == 0)
            {
                await tx.RollbackAsync();
                return Conflict(new { error = "Request is no longer Pending." });
            }

            await tx.CommitAsync();

            return Ok(new { id = request.Id, status = "Rejected", decidedAt, rejectionReason = string.IsNullOrEmpty(reason) ? null : reason });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
