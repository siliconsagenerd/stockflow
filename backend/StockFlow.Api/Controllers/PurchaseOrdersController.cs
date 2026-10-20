using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly StockFlowDbContext _db;

    public PurchaseOrdersController(StockFlowDbContext db) => _db = db;

    private async Task<(StockFlow.Api.Models.DemoSession? Session, IActionResult? Error)> ResolveSessionAsync()
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
}
