using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly StockFlowDbContext _db;
    private readonly ILogger<HealthController> _logger;

    public HealthController(StockFlowDbContext db, ILogger<HealthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<IActionResult> Get()
    {
        var dbOk = false;
        try
        {
            dbOk = await _db.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connectivity check failed");
        }

        var status = dbOk ? "Healthy" : "Unhealthy";
        var code = dbOk ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
        return StatusCode(code, new
        {
            status,
            process = "ok",
            database = dbOk ? "connected" : "unreachable",
            timestamp = DateTime.UtcNow
        });
    }
}