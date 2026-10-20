using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api;
using StockFlow.Api.Data;
using StockFlow.Api.DTOs;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/overview")]
public class OverviewController : ControllerBase
{
    private readonly StockFlowDbContext _db;

    public OverviewController(StockFlowDbContext db) => _db = db;

    private async Task<(StockFlow.Api.Models.DemoSession? Session, IActionResult? Error)> ResolveSessionAsync()
    {
        var session = await SessionContext.ResolveAsync(_db, HttpContext);
        if (session is null) return (null, Unauthorized(new { error = "No valid session." }));
        return (session, null);
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var (session, err) = await ResolveSessionAsync();
        if (err is not null) return err;
        var wsId = session!.WorkspaceId;

        var pendingRequests = await _db.PurchaseRequests.CountAsync(r => r.WorkspaceId == wsId && r.Status == "Pending");
        var openOrders = await _db.PurchaseOrders.CountAsync(o => o.PurchaseRequest.WorkspaceId == wsId && (o.Status == "Open" || o.Status == "PartiallyReceived"));
        var completedOrders = await _db.PurchaseOrders.CountAsync(o => o.PurchaseRequest.WorkspaceId == wsId && o.Status == "Completed");
        var receiptsRecorded = await _db.GoodsReceipts.CountAsync(g => g.WorkspaceId == wsId);
        var productsWithStock = await _db.StockBalances.Where(b => b.WorkspaceId == wsId && b.Quantity > 0).Select(b => b.ProductId).Distinct().CountAsync();

        // Recent activity: last 10 records across requests, orders, receipts
        var recentRequests = await _db.PurchaseRequests
            .Where(r => r.WorkspaceId == wsId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new { type = "request", id = r.Id, label = $"Request: {r.Product.Name} x{r.Quantity}", status = r.Status, at = r.CreatedAt })
            .ToListAsync();

        var recentOrders = await _db.PurchaseOrders
            .Where(o => o.PurchaseRequest.WorkspaceId == wsId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new { type = "order", id = o.Id, label = $"Order {o.Reference}: {o.PurchaseRequest.Product.Name}", status = o.Status, at = o.CreatedAt })
            .ToListAsync();

        var recentReceipts = await _db.GoodsReceipts
            .Where(g => g.WorkspaceId == wsId)
            .OrderByDescending(g => g.ReceivedAt)
            .Take(5)
            .Select(g => new { type = "receipt", id = g.Id, label = $"Receipt: {g.Quantity} units received", status = "received", at = g.ReceivedAt })
            .ToListAsync();

        var activity = recentRequests.Cast<object>()
            .Concat(recentOrders.Cast<object>())
            .Concat(recentReceipts.Cast<object>())
            .OrderByDescending(x => ((dynamic)x).at)
            .Take(10)
            .ToList();

        return Ok(new
        {
            pendingRequests,
            openOrders,
            completedOrders,
            receiptsRecorded,
            productsWithStock,
            recentActivity = activity
        });
    }
}
