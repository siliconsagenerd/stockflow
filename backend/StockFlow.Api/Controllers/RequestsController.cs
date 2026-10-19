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

    public RequestsController(StockFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        Guid wsId;
        try { wsId = DemoContext.ResolveWorkspaceId(HttpContext); }
        catch (DemoContextException e) { return BadRequest(new { error = e.Message }); }

        if (!await _db.DemoWorkspaces.AnyAsync(w => w.Id == wsId))
            return BadRequest(new { error = "Demo workspace not found." });

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
        Guid wsId;
        try { wsId = DemoContext.ResolveWorkspaceId(HttpContext); }
        catch (DemoContextException e) { return BadRequest(new { error = e.Message }); }

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
        Guid wsId;
        DemoUser requester;
        try
        {
            var ws = await DemoContext.ResolveWorkspaceAsync(_db, HttpContext);
            wsId = ws.Id;
            requester = await DemoContext.ResolveUserAsync(_db, HttpContext);
        }
        catch (DemoContextException e) { return BadRequest(new { error = e.Message }); }

        if (dto.Quantity < 1)
            return BadRequest(new { error = "Quantity must be a positive whole number." });

        if (dto.Justification is not null && dto.Justification.Length > MaxJustification)
            return BadRequest(new { error = $"Justification must be at most {MaxJustification} characters." });

        var product = await _db.Products.FindAsync(dto.ProductId);
        if (product is null)
            return BadRequest(new { error = "Unknown product." });

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
}