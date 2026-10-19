using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;

namespace StockFlow.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly StockFlowDbContext _db;

    public ProductsController(StockFlowDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var products = await _db.Products
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Sku, p.Name, p.Unit, p.CurrentStock })
            .ToListAsync();
        return Ok(products);
    }
}