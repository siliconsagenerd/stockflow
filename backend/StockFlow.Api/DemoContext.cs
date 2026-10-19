using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.Models;

namespace StockFlow.Api;

public static class DemoContext
{
    public const string UserHeader = "X-Demo-User";

    public static async Task<DemoUser> ResolveUserAsync(StockFlowDbContext db, HttpContext http)
    {
        var raw = http.Request.Headers[UserHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out var id))
            throw new DemoContextException("Missing or invalid demo identity.");
        var user = await db.DemoUsers.FindAsync(id);
        if (user is null)
            throw new DemoContextException("Demo identity not found.");
        return user;
    }
}

public class DemoContextException : Exception
{
    public DemoContextException(string message) : base(message) { }
}
