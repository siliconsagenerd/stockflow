using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/demo-users")]
public class DemoUsersController : ControllerBase
{
    private readonly StockFlowDbContext _db;

    public DemoUsersController(StockFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var users = await _db.DemoUsers
            .OrderBy(u => u.DisplayName)
            .Select(u => new { u.Id, u.DisplayName, u.Role })
            .ToListAsync();
        return Ok(users);
    }
}