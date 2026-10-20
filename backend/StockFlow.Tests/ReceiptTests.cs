using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.Models;

namespace StockFlow.Tests;

public class ReceiptTests : IClassFixture<StockFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly StockFlowWebApplicationFactory _factory;

    public ReceiptTests(StockFlowWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var db = _factory.CreateDbContext();
        db.Database.Migrate();
        db.StockMovements.RemoveRange(db.StockMovements);
        db.StockBalances.RemoveRange(db.StockBalances);
        db.GoodsReceipts.RemoveRange(db.GoodsReceipts);
        db.PurchaseOrders.RemoveRange(db.PurchaseOrders);
        db.PurchaseRequests.RemoveRange(db.PurchaseRequests);
        db.DemoSessions.RemoveRange(db.DemoSessions);
        db.DemoWorkspaces.RemoveRange(db.DemoWorkspaces);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient NewClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private void As(HttpClient client, Guid userId)
    {
        client.DefaultRequestHeaders.Remove("X-Demo-User");
        client.DefaultRequestHeaders.Add("X-Demo-User", userId.ToString());
    }

    private async Task<(HttpClient client, Guid wsId, Guid empId, Guid mgrId, Guid whId, Guid productId)> SetupAsync()
    {
        var client = NewClient();
        var sessionRes = await client.PostAsync("/api/session", null);
        sessionRes.EnsureSuccessStatusCode();
        var session = await sessionRes.Content.ReadFromJsonAsync<SessionDto>();

        var users = await client.GetFromJsonAsync<List<DemoUserDto>>("/api/demo-users") ?? new();
        var emp = users.First(u => u.Role == "Employee");
        var mgr = users.First(u => u.Role == "Manager");
        var wh = users.First(u => u.Role == "Warehouse");

        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products") ?? new();
        var product = products.First();

        return (client, session!.WorkspaceId, emp.Id, mgr.Id, wh.Id, product.Id);
    }

    private async Task<Guid> CreateApprovedPoAsync(HttpClient client, Guid productId, Guid empId, Guid mgrId, int qty = 10)
    {
        As(client, empId);
        var res = await client.PostAsJsonAsync("/api/requests", new { productId, quantity = qty });
        res.EnsureSuccessStatusCode();
        var req = await res.Content.ReadFromJsonAsync<RequestDto>();

        As(client, mgrId);
        var appr = await client.PostAsync($"/api/requests/{req!.Id}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        appr.EnsureSuccessStatusCode();
        var po = await appr.Content.ReadFromJsonAsync<PurchaseOrderDto>();
        return po!.Id;
    }

    [Fact]
    public async Task Receive6Of10_PartialStatusAndBalance()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(client, productId, empId, mgrId, 10);

        As(client, whId);
        var res = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 6, idempotencyKey = "k1" });
        res.EnsureSuccessStatusCode();
        var receipt = await res.Content.ReadFromJsonAsync<ReceiptDto>();
        Assert.NotNull(receipt);
        Assert.Equal(6, receipt!.Quantity);

        var po = await client.GetFromJsonAsync<PurchaseOrderDto>($"/api/purchase-orders/{poId}");
        Assert.Equal("PartiallyReceived", po!.Status);
        Assert.Equal(6, po.ReceivedQuantity);

        var inv = await client.GetFromJsonAsync<List<InventoryBalanceDto>>("/api/inventory");
        Assert.Contains(inv!, b => b.ProductId == productId && b.Quantity == 6);

        var moves = await client.GetFromJsonAsync<List<StockMovementDto>>("/api/stock-movements");
        Assert.Contains(moves!, m => m.Delta == 6 && m.Reason == "GoodsReceipt");
    }

    [Fact]
    public async Task ReceiveRemaining4_Completed()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(client, productId, empId, mgrId, 10);

        As(client, whId);
        await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 6, idempotencyKey = "k1" });
        var res = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 4, idempotencyKey = "k2" });
        res.EnsureSuccessStatusCode();

        var po = await client.GetFromJsonAsync<PurchaseOrderDto>($"/api/purchase-orders/{poId}");
        Assert.Equal("Completed", po!.Status);
        Assert.Equal(10, po.ReceivedQuantity);

        var inv = await client.GetFromJsonAsync<List<InventoryBalanceDto>>("/api/inventory");
        Assert.Contains(inv!, b => b.ProductId == productId && b.Quantity == 10);
    }

    [Fact]
    public async Task InvalidQuantity_Rejected()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(client, productId, empId, mgrId, 10);

        As(client, whId);
        var res = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 0, idempotencyKey = "k1" });
        Assert.False(res.IsSuccessStatusCode);
    }

    [Fact]
    public async Task OverReceipt_Rejected()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(client, productId, empId, mgrId, 10);

        As(client, whId);
        var res = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 11, idempotencyKey = "k1" });
        Assert.False(res.IsSuccessStatusCode);
    }

    [Fact]
    public async Task NonWarehouse_Rejected()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(client, productId, empId, mgrId, 10);

        As(client, empId);
        var res = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 6, idempotencyKey = "k1" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CrossWorkspace_AccessDenied()
    {
        var (clientA, wsA, empA, mgrA, whA, prodA) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(clientA, prodA, empA, mgrA, 10);

        var (clientB, _, _, _, whB, _) = await SetupAsync();
        As(clientB, whB);
        var res = await clientB.GetAsync($"/api/purchase-orders/{poId}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var recv = await clientB.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 1, idempotencyKey = "kx" });
        Assert.True(recv.StatusCode == HttpStatusCode.NotFound || recv.StatusCode == HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Idempotency_SameKeySamePayload_ReturnsOriginal()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(client, productId, empId, mgrId, 10);

        As(client, whId);
        var r1 = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 6, idempotencyKey = "dup" });
        r1.EnsureSuccessStatusCode();
        var receipt1 = await r1.Content.ReadFromJsonAsync<ReceiptDto>();

        var r2 = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 6, idempotencyKey = "dup" });
        r2.EnsureSuccessStatusCode();
        var receipt2 = await r2.Content.ReadFromJsonAsync<ReceiptDto>();

        Assert.Equal(receipt1!.Id, receipt2!.Id);
    }

    [Fact]
    public async Task Idempotency_SameKeyConflictingPayload_Rejected()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(client, productId, empId, mgrId, 10);

        As(client, whId);
        var r1 = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 6, idempotencyKey = "dup2" });
        r1.EnsureSuccessStatusCode();

        var r2 = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 4, idempotencyKey = "dup2" });
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task Idempotency_RetryAfterCompleted_ReturnsOriginal()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var poId = await CreateApprovedPoAsync(client, productId, empId, mgrId, 10);

        As(client, whId);
        await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 6, idempotencyKey = "a" });
        await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 4, idempotencyKey = "b" });

        // retry the first receipt after completion
        var r = await client.PostAsJsonAsync($"/api/purchase-orders/{poId}/receive", new { quantity = 6, idempotencyKey = "a" });
        r.EnsureSuccessStatusCode();
        var receipt = await r.Content.ReadFromJsonAsync<ReceiptDto>();
        Assert.Equal(6, receipt!.Quantity);
    }

    [Fact]
    public async Task DifferentOrders_SameProduct_NoLostIncrements()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        var po1 = await CreateApprovedPoAsync(client, productId, empId, mgrId, 5);
        var po2 = await CreateApprovedPoAsync(client, productId, empId, mgrId, 5);

        As(client, whId);
        await client.PostAsJsonAsync($"/api/purchase-orders/{po1}/receive", new { quantity = 5, idempotencyKey = "x1" });
        await client.PostAsJsonAsync($"/api/purchase-orders/{po2}/receive", new { quantity = 5, idempotencyKey = "x2" });

        var inv = await client.GetFromJsonAsync<List<InventoryBalanceDto>>("/api/inventory");
        var bal = inv!.First(b => b.ProductId == productId);
        Assert.Equal(10, bal.Quantity);
    }

    [Fact]
    public async Task ZeroOpeningStock_NoDuplicate()
    {
        var (client, wsId, empId, mgrId, whId, productId) = await SetupAsync();
        // inventory should be empty initially (zero opening balances)
        var inv = await client.GetFromJsonAsync<List<InventoryBalanceDto>>("/api/inventory");
        Assert.Empty(inv!);
    }

    private record SessionDto(Guid WorkspaceId);
    private record DemoUserDto(Guid Id, string DisplayName, string Role);
    private record ProductDto(Guid Id, string Sku, string Name, string Unit, int CurrentStock);
    private record RequestDto(Guid Id, Guid ProductId, string ProductName, string ProductSku, string ProductUnit, int Quantity, string? Justification, string Status, Guid RequesterId, string RequesterName, DateTime CreatedAt, Guid WorkspaceId);
    private record PurchaseOrderDto(Guid Id, string Reference, Guid PurchaseRequestId, Guid SupplierId, string SupplierName, Guid WarehouseId, string WarehouseName, Guid ProductId, string ProductName, string ProductSku, int OrderedQuantity, int ReceivedQuantity, string Status, DateTime CreatedAt);
    private record ReceiptDto(Guid Id, Guid PurchaseOrderId, Guid WorkspaceId, int Quantity, Guid ReceivedById, string ReceivedByName, DateTime ReceivedAt, string IdempotencyKey, int OrderReceivedQuantity, string OrderStatus);
    private record InventoryBalanceDto(Guid ProductId, string ProductName, string ProductSku, string ProductUnit, Guid WarehouseId, string WarehouseName, int Quantity);
    private record StockMovementDto(Guid Id, Guid ProductId, string ProductName, Guid WarehouseId, string WarehouseName, int Delta, string Reason, Guid? ReferenceId, DateTime CreatedAt, Guid WorkspaceId);
}
