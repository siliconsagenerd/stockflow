using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using StockFlow.Api.Data;
using Microsoft.EntityFrameworkCore;
using StockFlow.Api.Models;

namespace StockFlow.Tests;

public class RequestCreationTests : IClassFixture<StockFlowWebApplicationFactory>, IAsyncLifetime
{
    private readonly StockFlowWebApplicationFactory _factory;

    public RequestCreationTests(StockFlowWebApplicationFactory factory)
    {
        _factory = factory;
    }

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

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private async Task<(HttpClient client, Guid workspaceId, Guid userId, Guid productId)> SetupAsync()
    {
        var client = NewClient();
        var sessionRes = await client.PostAsync("/api/session", null);
        sessionRes.EnsureSuccessStatusCode();
        var session = await sessionRes.Content.ReadFromJsonAsync<SessionDto>();

        var users = await client.GetFromJsonAsync<List<DemoUserDto>>("/api/demo-users") ?? new();
        var employee = users.First(u => u.Role == "Employee");

        var products = await client.GetFromJsonAsync<List<ProductDto>>("/api/products") ?? new();
        var product = products.First();

        return (client, session!.WorkspaceId, employee.Id, product.Id);
    }

    private HttpRequestMessage Req(HttpMethod method, string url, Guid userId, object? body = null, string? workspaceOverride = null)
    {
        var msg = new HttpRequestMessage(method, url);
        msg.Headers.Add("X-Demo-User", userId.ToString());
        if (body is not null) msg.Content = JsonContent.Create(body);
        return msg;
    }

    [Fact]
    public async Task ValidRequest_PersistedAsPending()
    {
        var (client, wsId, userId, productId) = await SetupAsync();
        var res = await client.SendAsync(Req(HttpMethod.Post, "/api/requests", userId,
            new { productId, quantity = 7, justification = "need parts" }));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<RequestDto>();
        Assert.NotNull(dto);
        Assert.Equal("Pending", dto!.Status);
        Assert.Equal(7, dto.Quantity);
        Assert.Equal(productId, dto.ProductId);
        Assert.Equal(userId, dto.RequesterId);
        Assert.Equal(wsId, dto.WorkspaceId);

        using var db = _factory.CreateDbContext();
        var entity = await db.PurchaseRequests.FindAsync(dto.Id);
        Assert.NotNull(entity);
        Assert.Equal("Pending", entity!.Status);
    }

    [Fact]
    public async Task InvalidQuantities_Rejected()
    {
        var (client, _, userId, productId) = await SetupAsync();
        foreach (var q in new[] { 0, -1, -5 })
        {
            var res = await client.SendAsync(Req(HttpMethod.Post, "/api/requests", userId,
                new { productId, quantity = q }));
            Assert.False(res.IsSuccessStatusCode);
        }
    }

    [Fact]
    public async Task FractionalQuantity_Rejected()
    {
        var (client, _, userId, productId) = await SetupAsync();
        var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/requests")
        {
            Headers = { { "X-Demo-User", userId.ToString() } },
            Content = new StringContent("{\"productId\":\"" + productId + "\",\"quantity\":1.5}", System.Text.Encoding.UTF8, "application/json")
        });
        Assert.False(res.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task UnknownProduct_And_InvalidContext_Rejected()
    {
        var (client, _, userId, productId) = await SetupAsync();

        var res1 = await client.SendAsync(Req(HttpMethod.Post, "/api/requests", userId,
            new { productId = Guid.NewGuid(), quantity = 3 }));
        Assert.False(res1.IsSuccessStatusCode);

        // no session client
        var noSessionClient = NewClient();
        var res2 = await noSessionClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/requests")
        {
            Headers = { { "X-Demo-User", userId.ToString() } },
            Content = JsonContent.Create(new { productId, quantity = 3 })
        });
        Assert.Equal(HttpStatusCode.Unauthorized, res2.StatusCode);
    }

    [Fact]
    public async Task OverlongJustification_Rejected()
    {
        var (client, _, userId, productId) = await SetupAsync();
        var longText = new string('a', 501);
        var res = await client.SendAsync(Req(HttpMethod.Post, "/api/requests", userId,
            new { productId, quantity = 2, justification = longText }));
        Assert.False(res.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ListAndDetail_ScopedToSession()
    {
        var (client, wsId, userId, productId) = await SetupAsync();
        var res = await client.SendAsync(Req(HttpMethod.Post, "/api/requests", userId,
            new { productId, quantity = 4 }));
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<RequestDto>();

        var list = await client.SendAsync(Req(HttpMethod.Get, "/api/requests", userId));
        list.EnsureSuccessStatusCode();
        var items = await list.Content.ReadFromJsonAsync<List<RequestDto>>() ?? new();
        Assert.Contains(items, r => r.Id == created!.Id);

        var detail = await client.SendAsync(Req(HttpMethod.Get, $"/api/requests/{created!.Id}", userId));
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
    }

    [Fact]
    public async Task CrossSession_CannotRetrieveById()
    {
        var (clientA, _, userIdA, productId) = await SetupAsync();
        var res = await clientA.SendAsync(Req(HttpMethod.Post, "/api/requests", userIdA,
            new { productId, quantity = 2 }));
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<RequestDto>();

        // second independent session
        var (clientB, _, _, _) = await SetupAsync();
        var cross = await clientB.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/api/requests/{created!.Id}")
        {
            Headers = { { "X-Demo-User", userIdA.ToString() } }
        });
        Assert.Equal(HttpStatusCode.NotFound, cross.StatusCode);
    }

    [Fact]
    public async Task TwoIndependentSessions_IsolatedRequests()
    {
        var (clientA, wsA, userA, productA) = await SetupAsync();
        var (clientB, wsB, userB, productB) = await SetupAsync();

        Assert.NotEqual(wsA, wsB);

        await clientA.SendAsync(Req(HttpMethod.Post, "/api/requests", userA,
            new { productId = productA, quantity = 1 }));
        await clientB.SendAsync(Req(HttpMethod.Post, "/api/requests", userB,
            new { productId = productB, quantity = 2 }));

        var listA = await (await clientA.SendAsync(Req(HttpMethod.Get, "/api/requests", userA))).Content.ReadFromJsonAsync<List<RequestDto>>() ?? new();
        var listB = await (await clientB.SendAsync(Req(HttpMethod.Get, "/api/requests", userB))).Content.ReadFromJsonAsync<List<RequestDto>>() ?? new();

        Assert.Single(listA);
        Assert.Single(listB);
        Assert.NotEqual(listA[0].Id, listB[0].Id);
    }

    [Fact]
    public async Task MissingOrInvalidSession_CannotReadOrMutate()
    {
        var noSession = NewClient();
        var userId = Guid.NewGuid();

        var get = await noSession.GetAsync("/api/requests");
        Assert.Equal(HttpStatusCode.Unauthorized, get.StatusCode);

        var getOne = await noSession.GetAsync($"/api/requests/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, getOne.StatusCode);

        var post = await noSession.PostAsJsonAsync("/api/requests", new { productId = Guid.NewGuid(), quantity = 1 });
        Assert.Equal(HttpStatusCode.Unauthorized, post.StatusCode);
    }

    private record SessionDto(Guid WorkspaceId);
    private record DemoUserDto(Guid Id, string DisplayName, string Role);
    private record ProductDto(Guid Id, string Sku, string Name, string Unit, int CurrentStock);
    private record RequestDto(Guid Id, Guid ProductId, string ProductName, string ProductSku, string ProductUnit,
        int Quantity, string? Justification, string Status, Guid RequesterId, string RequesterName,
        DateTime CreatedAt, Guid WorkspaceId);
}
