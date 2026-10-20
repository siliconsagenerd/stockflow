using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api")]
public class InventoryController : ControllerBase
{
    private readonly StockFlowDbContext _db;

    public InventoryController(StockFlowDbContext db) => _db = db;

    private async Task<(StockFlow.Api.Models.DemoSession? Session, IActionResult? Error)> ResolveSessionAsync()
    {
        var session = await SessionContext.ResolveAsync(_db, HttpContext);
        if (session is null) return (null, Unauthorized(new { error = "No valid session." }));
        return (session, null);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Balances()
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        var items = await _db.StockBalances
            .Where(b => b.WorkspaceId == wsId)
            .Select(b => new InventoryBalanceDto(
                b.ProductId, b.Product.Name, b.Product.Sku, b.Product.Unit,
                b.WarehouseId, b.Warehouse.Name, b.Quantity))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("stock-movements")]
    public async Task<IActionResult> Movements()
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        var items = await _db.StockMovements
            .Where(m => m.WorkspaceId == wsId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new StockMovementDto(
                m.Id, m.ProductId, m.Product.Name, m.WarehouseId, m.Warehouse.Name,
                m.Delta, m.Reason, m.ReferenceId, m.CreatedAt, m.WorkspaceId))
            .ToListAsync();

        return Ok(items);
    }
}
