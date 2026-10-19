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
    private readonly IWebHostEnvironment _env;

    public WorkspacesController(StockFlowDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var (ws, session, token) = await SessionManager.CreateAsync(_db);
        SessionManager.SetCookie(Response, token, session.ExpiresAt, _env);
        return CreatedAtAction(nameof(Get), new { id = ws.Id }, new { id = ws.Id, createdAt = ws.CreatedAt });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var session = await SessionContext.ResolveAsync(_db, HttpContext);
        if (session is null) return Unauthorized(new { error = "No valid session." });
        if (session.WorkspaceId != id) return NotFound(new { error = "Workspace not found." });
        var ws = await _db.DemoWorkspaces.FindAsync(id);
        if (ws is null) return NotFound(new { error = "Workspace not found." });
        return Ok(new { id = ws.Id, createdAt = ws.CreatedAt });
    }
}
