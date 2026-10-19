using System.Security.Cryptography;
using System.Text;

namespace StockFlow.Api.Models;

public class DemoSession
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = "";
    public Guid WorkspaceId { get; set; }
    public DemoWorkspace Workspace { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
