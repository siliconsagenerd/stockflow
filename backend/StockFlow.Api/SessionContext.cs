using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.Models;

namespace StockFlow.Api;

public static class SessionContext
{
    public const string CookieName = "stockflow_session";
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    public static async Task<DemoSession?> ResolveAsync(StockFlowDbContext db, HttpContext http)
    {
        var token = http.Request.Cookies[CookieName];
        if (string.IsNullOrWhiteSpace(token)) return null;
        var hash = DemoSession.HashToken(token);
        var session = await db.DemoSessions.Include(s => s.Workspace)
            .FirstOrDefaultAsync(s => s.TokenHash == hash);
        if (session is null || session.ExpiresAt < DateTime.UtcNow) return null;
        session.Workspace.LastAccessedAt = DateTime.UtcNow;
        return session;
    }
}

public static class SessionManager
{
    public static async Task<(DemoWorkspace Ws, DemoSession Session, string Token)> CreateAsync(StockFlowDbContext db)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var ws = new DemoWorkspace { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow, LastAccessedAt = DateTime.UtcNow };
        db.DemoWorkspaces.Add(ws);
        var session = new DemoSession
        {
            Id = Guid.NewGuid(),
            TokenHash = DemoSession.HashToken(token),
            WorkspaceId = ws.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(SessionContext.Lifetime)
        };
        db.DemoSessions.Add(session);
        await db.SaveChangesAsync();
        return (ws, session, token);
    }

    public static void SetCookie(HttpResponse response, string token, DateTime expires, IWebHostEnvironment env)
    {
        response.Cookies.Append(SessionContext.CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !env.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = expires
        });
    }
}
