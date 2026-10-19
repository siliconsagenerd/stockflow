using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/workspaces")]
public class WorkspacesController : ControllerBase
{
    private readonly StockFlowDbContext _db;

    public WorkspacesController(StockFlowDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var ws = new DemoWorkspace { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, LastAccessedAt = DateTime.UtcNow };
        _db.DemoWorkspaces.Add(ws);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = ws.Id }, new { id = ws.Id, createdAt = ws.CreatedAt });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var ws = await _db.DemoWorkspaces.FindAsync(id);
        if (ws is null) return NotFound(new { error = "Workspace not found." });
        return Ok(new { id = ws.Id, createdAt = ws.CreatedAt });
    }
}