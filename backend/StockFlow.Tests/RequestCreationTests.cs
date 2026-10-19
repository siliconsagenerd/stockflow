using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StockFlow.Api.Data;
using StockFlow.Api.Models;

namespace StockFlow.Tests;

public class RequestCreationTests : IClassFixture<StockFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly StockFlowWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RequestCreationTests(StockFlowWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Clean workflow tables only (test DB).
        using var db = _factory.CreateDbContext();
        db.PurchaseRequests.RemoveRange(db.PurchaseRequests);
        db.DemoWorkspaces.RemoveRange(db.DemoWorkspaces);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(Guid wsId, Guid userId, Guid productId)> SetupAsync()
    {
        var wsRes = await _client.PostAsync("/api/workspaces", null);
        wsRes.EnsureSuccessStatusCode();
        var ws = await wsRes.Content.ReadFromJsonAsync<WorkspaceDto>();

        var users = await _client.GetFromJsonAsync<List<DemoUserDto>>("/api/demo-users") ?? new();
        var employee = users.First(u => u.Role == "Employee");

        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products") ?? new();
        var product = products.First();

        return (ws!.Id, employee.Id, product.Id);
    }

    private HttpRequestMessage Req(HttpMethod method, string url, Guid wsId, Guid userId, object? body = null)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Add("X-Demo-Workspace", wsId.ToString());
        msg.Headers.Add("X-Demo-User", userId.ToString());
        if (body is not null)
        {
            msg.Content = JsonContent.Create(body);
        }
        return msg;
    }

    [Fact]
    public async Task ValidRequest_PersistedAsPending()
    {
        var (wsId, userId, productId) = await SetupAsync();
        var res = await _client.SendAsync(Req(HttpMethod.Post, "/api/requests", wsId, userId,
            new { productId, quantity = 7, justification = "need parts" }));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<RequestDto>();
        Assert.NotNull(dto);
        Assert.Equal("Pending", dto!.Status);
        Assert.Equal(7, dto.Quantity);
        Assert.Equal(productId, dto.ProductId);
        Assert.Equal(userId, dto.RequesterId);
        Assert.Equal(wsId, dto.WorkspaceId);

        // Verify persisted
        using var db = _factory.CreateDbContext();
        var entity = await db.PurchaseRequests.FindAsync(dto.Id);
        Assert.NotNull(entity);
        Assert.Equal("Pending", entity!.Status);
    }

    [Fact]
    public async Task InvalidQuantities_Rejected()
    {
        var (wsId, userId, productId) = await SetupAsync();
        foreach (var q in new[] { 0, -1, -5 })
        {
            var res = await _client.SendAsync(Req(HttpMethod.Post, "/api/requests", wsId, userId,
                new { productId, quantity = q }));
            Assert.False(res.IsSuccessStatusCode);
        }
    }

    [Fact]
    public async Task UnknownProduct_And_InvalidContext_Rejected()
    {
        var (wsId, userId, productId) = await SetupAsync();

        // unknown product
        var res1 = await _client.SendAsync(Req(HttpMethod.Post, "/api/requests", wsId, userId,
            new { productId = Guid.NewGuid(), quantity = 3 }));
        Assert.False(res1.IsSuccessStatusCode);

        // invalid workspace
        var res2 = await _client.SendAsync(Req(HttpMethod.Post, "/api/requests", Guid.NewGuid(), userId,
            new { productId, quantity = 3 }));
        Assert.False(res2.IsSuccessStatusCode);

        // invalid user
        var res3 = await _client.SendAsync(Req(HttpMethod.Post, "/api/requests", wsId, Guid.NewGuid(),
            new { productId, quantity = 3 }));
        Assert.False(res3.IsSuccessStatusCode);
    }

    [Fact]
    public async Task OverlongJustification_Rejected()
    {
        var (wsId, userId, productId) = await SetupAsync();
        var longText = new string('a', 501);
        var res = await _client.SendAsync(Req(HttpMethod.Post, "/api/requests", wsId, userId,
            new { productId, quantity = 2, justification = longText }));
        Assert.False(res.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ListAndDetail_ScopedToWorkspace()
    {
        var (wsId, userId, productId) = await SetupAsync();
        var res = await _client.SendAsync(Req(HttpMethod.Post, "/api/requests", wsId, userId,
            new { productId, quantity = 4 }));
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<RequestDto>();

        var listRes = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/requests")
        {
            Headers = { { "X-Demo-Workspace", wsId.ToString() } }
        });
        // include user header
        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/requests");
        listReq.Headers.Add("X-Demo-Workspace", wsId.ToString());
        listReq.Headers.Add("X-Demo-User", userId.ToString());
        var list = await _client.SendAsync(listReq);
        list.EnsureSuccessStatusCode();
        var items = await list.Content.ReadFromJsonAsync<List<RequestDto>>() ?? new();
        Assert.Contains(items, r => r.Id == created!.Id);

        var detail = await _client.SendAsync(Req(HttpMethod.Get, $"/api/requests/{created!.Id}", wsId, userId));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task CrossWorkspace_CannotRetrieveById()
    {
        var (wsId, userId, productId) = await SetupAsync();
        var res = await _client.SendAsync(Req(HttpMethod.Post, "/api/requests", wsId, userId,
            new { productId, quantity = 2 }));
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<RequestDto>();

        // new workspace
        var ws2Res = await _client.PostAsync("/api/workspaces", null);
        ws2Res.EnsureSuccessStatusCode();
        var ws2 = await ws2Res.Content.ReadFromJsonAsync<WorkspaceDto>();

        var cross = await _client.SendAsync(Req(HttpMethod.Get, $"/api/requests/{created!.Id}", ws2!.Id, userId));
        Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
    }

    private record WorkspaceDto(Guid Id, DateTime CreatedAt);
    private record DemoUserDto(Guid Id, string DisplayName, string Role);
    private record ProductDto(Guid Id, string Sku, string Name, string Unit, int CurrentStock);
    private record RequestDto(Guid Id, Guid ProductId, string ProductName, string ProductSku, string ProductUnit,
        int Quantity, string? Justification, string Status, Guid RequesterId, string RequesterName,
        DateTime CreatedAt, Guid WorkspaceId);
}