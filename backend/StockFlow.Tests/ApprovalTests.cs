using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Data;
using StockFlow.Api.Models;

namespace StockFlow.Tests;

public class ApprovalTests : IClassFixture<StockFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly StockFlowWebApplicationFactory _factory;

    public ApprovalTests(StockFlowWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var db = _factory.CreateDbContext();
        db.Database.Migrate();
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

    private async Task<(HttpClient client, Guid wsId, Guid empId, Guid mgrId, Guid productId)> SetupAsync()
    {
        var client = NewClient();
        var sessionRes = await client.PostAsync("/api/session", null);
        sessionRes.EnsureSuccessStatusCode();
        var session = await sessionRes.Content.ReadFromJsonAsync<SessionDto>();

        var users = await client.GetFromJsonAsync<List<DemoUserDto>>("/api/demo-users") ?? new();
        var emp = users.First(u => u.Role == "Employee");
        var mgr = users.First(u => u.Role == "Manager");

        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products") ?? new();
        var product = products.First();

        return (client, session!.WorkspaceId, emp.Id, mgr.Id, product.Id);
    }

    private async Task<Guid> CreateRequestAsync(HttpClient client, Guid productId, Guid empId)
    {
        As(client, empId);
        var res = await client.PostAsJsonAsync("/api/requests", new { productId, quantity = 4 });
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<RequestDto>();
        return dto!.Id;
    }

    [Fact]
    public async Task ManagerApproves_CreatesOrder()
    {
        var (client, wsId, empId, mgrId, productId) = await SetupAsync();
        var rid = await CreateRequestAsync(client, productId, empId);

        As(client, mgrId);
        var res = await client.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var po = await res.Content.ReadFromJsonAsync<PurchaseOrderDto>();
        Assert.NotNull(po);
        Assert.Equal(rid, po!.PurchaseRequestId);
        Assert.Equal("Open", po.Status);
        Assert.Equal(4, po.OrderedQuantity);

        As(client, mgrId);
        var r2 = await client.GetAsync($"/api/requests/{rid}");
        var rd = await r2.Content.ReadFromJsonAsync<RequestDto>();
        Assert.Equal("Approved", rd!.Status);

        var listRes = await client.GetAsync("/api/purchase-orders");
        listRes.EnsureSuccessStatusCode();
        var orders = await listRes.Content.ReadFromJsonAsync<List<PurchaseOrderDto>>() ?? new();
        Assert.Contains(orders, o => o.Id == po.Id);
    }

    [Fact]
    public async Task SelfApproval_Rejected()
    {
        var (client, _, empId, _, productId) = await SetupAsync();
        var rid = await CreateRequestAsync(client, productId, empId);
        As(client, empId);
        var res = await client.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task NonManager_Rejected()
    {
        var (client, _, empId, _, productId) = await SetupAsync();
        var rid = await CreateRequestAsync(client, productId, empId);
        As(client, empId);
        var res = await client.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Rejection_CreatesNoOrder()
    {
        var (client, _, empId, mgrId, productId) = await SetupAsync();
        var rid = await CreateRequestAsync(client, productId, empId);
        As(client, mgrId);
        var res = await client.PostAsJsonAsync($"/api/requests/{rid}/reject", new { reason = "Not needed" });
        Assert.True(res.IsSuccessStatusCode);

        var ordersRes = await client.GetAsync("/api/purchase-orders");
        var orders = await ordersRes.Content.ReadFromJsonAsync<List<PurchaseOrderDto>>() ?? new();
        Assert.DoesNotContain(orders, o => o.PurchaseRequestId == rid);
    }

    [Fact]
    public async Task RepeatedApproval_NoDuplicateOrder()
    {
        var (client, _, empId, mgrId, productId) = await SetupAsync();
        var rid = await CreateRequestAsync(client, productId, empId);
        As(client, mgrId);
        var first = await client.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.False(second.IsSuccessStatusCode);

        var ordersRes = await client.GetAsync("/api/purchase-orders");
        var orders = await ordersRes.Content.ReadFromJsonAsync<List<PurchaseOrderDto>>() ?? new();
        Assert.Single(orders.Where(o => o.PurchaseRequestId == rid));
    }

    [Fact]
    public async Task ConcurrentApprovals_ExactlyOneOrder()
    {
        var (client, _, empId, mgrId, productId) = await SetupAsync();
        var rid = await CreateRequestAsync(client, productId, empId);
        As(client, mgrId);

        var t1 = client.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        var t2 = client.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        await Task.WhenAll(t1, t2);
        Assert.True(t1.Result.IsSuccessStatusCode || t2.Result.IsSuccessStatusCode);

        var ordersRes = await client.GetAsync("/api/purchase-orders");
        var orders = await ordersRes.Content.ReadFromJsonAsync<List<PurchaseOrderDto>>() ?? new();
        Assert.Single(orders.Where(o => o.PurchaseRequestId == rid));
    }

    [Fact]
    public async Task ConcurrentApproveReject_ConsistentState()
    {
        var (client, _, empId, mgrId, productId) = await SetupAsync();
        var rid = await CreateRequestAsync(client, productId, empId);
        As(client, mgrId);

        var t1 = client.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        var t2 = client.PostAsJsonAsync($"/api/requests/{rid}/reject", new { reason = "no" });
        await Task.WhenAll(t1, t2);
        Assert.True(t1.Result.IsSuccessStatusCode || t2.Result.IsSuccessStatusCode);

        var r = await client.GetAsync($"/api/requests/{rid}");
        var rd = await r.Content.ReadFromJsonAsync<RequestDto>();
        Assert.True(rd!.Status == "Approved" || rd.Status == "Rejected");
    }

    [Fact]
    public async Task MissingSession_Rejected()
    {
        var noSession = NewClient();
        var res = await noSession.PostAsync($"/api/requests/{Guid.NewGuid()}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task CrossWorkspace_AccessDenied()
    {
        var (clientA, _, empA, mgrA, prodA) = await SetupAsync();
        var rid = await CreateRequestAsync(clientA, prodA, empA);
        As(clientA, mgrA);
        var appr = await clientA.PostAsync($"/api/requests/{rid}/approve", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        appr.EnsureSuccessStatusCode();

        var (clientB, _, _, _, _) = await SetupAsync();
        As(clientB, mgrA);
        var res = await clientB.GetAsync($"/api/requests/{rid}");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);

        var poRes = await clientB.GetAsync("/api/purchase-orders");
        poRes.EnsureSuccessStatusCode();
        var orders = await poRes.Content.ReadFromJsonAsync<List<PurchaseOrderDto>>() ?? new();
        Assert.DoesNotContain(orders, o => o.PurchaseRequestId == rid);
    }

    private record SessionDto(Guid WorkspaceId);
    private record DemoUserDto(Guid Id, string DisplayName, string Role);
    private record ProductDto(Guid Id, string Sku, string Name, string Unit, int CurrentStock);
    private record RequestDto(Guid Id, Guid ProductId, string ProductName, string ProductSku, string ProductUnit, int Quantity, string? Justification, string Status, Guid RequesterId, string RequesterName, DateTime CreatedAt, Guid WorkspaceId);
    private record PurchaseOrderDto(Guid Id, string Reference, Guid PurchaseRequestId, Guid SupplierId, string SupplierName, Guid WarehouseId, string WarehouseName, Guid ProductId, string ProductName, string ProductSku, int OrderedQuantity, int ReceivedQuantity, string Status, DateTime CreatedAt);
}
