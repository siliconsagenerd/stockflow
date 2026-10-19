using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.Models;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/session")]
public class SessionsController : ControllerBase
{
    private readonly StockFlowDbContext _db;
    private readonly IWebHostEnvironment _env;

    public SessionsController(StockFlowDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
        var (ws, session, token) = await SessionManager.CreateAsync(_db);
        SessionManager.SetCookie(Response, token, session.ExpiresAt, _env);
        return Ok(new { workspaceId = ws.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var session = await SessionContext.ResolveAsync(_db, HttpContext);
        if (session is null) return Unauthorized(new { error = "No valid session." });
        return Ok(new { workspaceId = session.WorkspaceId });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        var session = await SessionContext.ResolveAsync(_db, HttpContext);
        if (session is not null)
        {
            _db.DemoSessions.Remove(session);
            await _db.SaveChangesAsync();
        }
        Response.Cookies.Delete(SessionContext.CookieName, new CookieOptions { Path = "/" });
        return NoContent();
    }
}
