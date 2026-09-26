using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TillApp.Server.Data;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Orders;

namespace TillApp.Server.Tests;

[Collection(SqlServerApiCollection.Name)]
public sealed class OrdersApiTests(OrderApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TillAppDbContext>();
        await dbContext.Database.MigrateAsync();
        await dbContext.OrderItems.ExecuteDeleteAsync();
        await dbContext.Orders.ExecuteDeleteAsync();
        await dbContext.Products.ExecuteDeleteAsync();
        await dbContext.Categories.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_ValidOrderCreatesPendingOrderWithSnapshotsAndTimestamps()
    {
        var burger = await AddProductAsync("Food", "Burger", 5.00m);

        var response = await _client.PostAsJsonAsync("/api/orders", CreateRequest("Lunch", (burger.ProductId, 3)));
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.False(order.IsPaid);
        Assert.NotEqual(default, order.CreatedAt);
        Assert.Null(order.PaidAt);
        Assert.Null(order.CancelledAt);
        Assert.Equal(15.00m, order.Amount);
        var item = Assert.Single(order.Items);
        Assert.Equal(burger.ProductId, item.ProductId);
        Assert.Equal("Burger", item.ProductName);
        Assert.Equal(5.00m, item.UnitPrice);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(15.00m, item.LineTotal);
    }

    [Fact]
    public async Task Create_UsesAuthoritativeProductNamePriceAndServerCalculatedTotal()
    {
        var tea = await AddProductAsync("Drinks", "Tea", 2.40m);
        var request = new CreateOrderRequest
        {
            OrderName = "Price override attempt",
            Items =
            [
                new CreateOrderItemRequest
                {
                    ProductId = tea.ProductId,
                    ItemName = "Client supplied name",
                    Price = 0.01m,
                    Quantity = 2
                }
            ]
        };

        var order = await CreateOrderAsync(request);

        Assert.Equal(4.80m, order.Amount);
        var item = Assert.Single(order.Items);
        Assert.Equal("Tea", item.ProductName);
        Assert.Equal(2.40m, item.UnitPrice);
    }

    [Fact]
    public async Task Create_CombinesRepeatedProductLinesIntoOneQuantity()
    {
        var fries = await AddProductAsync("Food", "Fries", 3.25m);
        var request = CreateRequest("Shared order", (fries.ProductId, 1), (fries.ProductId, 2));

        var order = await CreateOrderAsync(request);

        var item = Assert.Single(order.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(9.75m, order.Amount);
    }

    [Fact]
    public async Task Create_PreservesProductSnapshotsAfterProductChanges()
    {
        var product = await AddProductAsync("Food", "Burger", 5.00m);
        var order = await CreateOrderAsync(CreateRequest("Lunch", (product.ProductId, 1)));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TillAppDbContext>();
            var persistedProduct = await dbContext.Products.SingleAsync(candidate => candidate.ProductId == product.ProductId);
            persistedProduct.Name = "Classic Burger";
            persistedProduct.UnitPrice = 6.00m;
            await dbContext.SaveChangesAsync();
        }

        var retrieved = await _client.GetFromJsonAsync<OrderDto>($"/api/orders/{order.OrderId}");

        Assert.NotNull(retrieved);
        var item = Assert.Single(retrieved.Items);
        Assert.Equal("Burger", item.ProductName);
        Assert.Equal(5.00m, item.UnitPrice);
        Assert.Equal(5.00m, retrieved.Amount);
    }

    [Fact]
    public async Task Create_AllowsExistingStaticClientRequestWhenProductNameIsUnique()
    {
        await AddProductAsync("Drinks", "Tea", 2.40m);
        var request = new CreateOrderRequest
        {
            OrderName = "Legacy client",
            Items = [new CreateOrderItemRequest { ItemName = "Tea", Price = 99.99m }]
        };

        var order = await CreateOrderAsync(request);

        Assert.Equal(2.40m, order.Amount);
        Assert.Equal("Tea", order.Items[0].ProductName);
    }

    [Fact]
    public async Task Create_InactiveProductReturnsBadRequestProblem()
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m, isActive: false);

        var response = await _client.PostAsJsonAsync("/api/orders", CreateRequest("Lunch", (product.ProductId, 1)));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_UnknownProductReturnsBadRequestProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", CreateRequest("Lunch", (2147483647, 1)));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_NonPositiveQuantityReturnsValidationProblem(int quantity)
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m);

        var response = await _client.PostAsJsonAsync("/api/orders", CreateRequest("Lunch", (product.ProductId, quantity)));

        await AssertValidationProblemAsync(response, "Items[0].Quantity");
    }

    [Fact]
    public async Task Create_EmptyOrderReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            OrderName = "Empty",
            Items = []
        });

        await AssertValidationProblemAsync(response, "Items");
    }

    [Fact]
    public async Task MarkPaid_TransitionsPendingOrderAndIsIdempotent()
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m);
        var created = await CreateOrderAsync(CreateRequest("Lunch", (product.ProductId, 1)));

        var firstResponse = await _client.PatchAsync($"/api/orders/{created.OrderId}/paid", null);
        var paid = await firstResponse.Content.ReadFromJsonAsync<OrderDto>();
        var secondResponse = await _client.PatchAsync($"/api/orders/{created.OrderId}/paid", null);
        var paidAgain = await secondResponse.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(OrderStatus.Paid, paid!.Status);
        Assert.NotNull(paid.PaidAt);
        Assert.Equal(paid.PaidAt, paidAgain!.PaidAt);
    }

    [Fact]
    public async Task Cancel_TransitionsPendingOrderAndIsIdempotent()
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m);
        var created = await CreateOrderAsync(CreateRequest("Lunch", (product.ProductId, 1)));

        var firstResponse = await _client.PatchAsync($"/api/orders/{created.OrderId}/cancel", null);
        var cancelled = await firstResponse.Content.ReadFromJsonAsync<OrderDto>();
        var secondResponse = await _client.PatchAsync($"/api/orders/{created.OrderId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
        Assert.NotNull(cancelled.CancelledAt);
    }

    [Fact]
    public async Task Cancel_PaidOrderReturnsConflict()
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m);
        var created = await CreateOrderAsync(CreateRequest("Lunch", (product.ProductId, 1)));
        await _client.PatchAsync($"/api/orders/{created.OrderId}/paid", null);

        var response = await _client.PatchAsync($"/api/orders/{created.OrderId}/cancel", null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task MarkPaid_CancelledOrderReturnsConflict()
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m);
        var created = await CreateOrderAsync(CreateRequest("Lunch", (product.ProductId, 1)));
        await _client.PatchAsync($"/api/orders/{created.OrderId}/cancel", null);

        var response = await _client.PatchAsync($"/api/orders/{created.OrderId}/paid", null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetOrders_SupportsStatusFilterAndLegacyUnpaidFilter()
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m);
        var pending = await CreateOrderAsync(CreateRequest("Pending", (product.ProductId, 1)));
        var paid = await CreateOrderAsync(CreateRequest("Paid", (product.ProductId, 1)));
        var cancelled = await CreateOrderAsync(CreateRequest("Cancelled", (product.ProductId, 1)));
        await _client.PatchAsync($"/api/orders/{paid.OrderId}/paid", null);
        await _client.PatchAsync($"/api/orders/{cancelled.OrderId}/cancel", null);

        var paidOrders = await _client.GetFromJsonAsync<List<OrderDto>>("/api/orders?status=Paid");
        var legacyUnpaid = await _client.GetFromJsonAsync<List<OrderDto>>("/api/orders?isPaid=false");

        Assert.Equal(paid.OrderId, Assert.Single(paidOrders!).OrderId);
        Assert.Equal(pending.OrderId, Assert.Single(legacyUnpaid!).OrderId);
    }

    [Fact]
    public async Task GetOrders_CombinesStatusSearchAndInclusiveUtcDateRange()
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m);
        var matching = await CreateOrderAsync(CreateRequest("Table 4", (product.ProductId, 1)));
        var wrongStatus = await CreateOrderAsync(CreateRequest("Table 5", (product.ProductId, 1)));
        var wrongName = await CreateOrderAsync(CreateRequest("Takeaway", (product.ProductId, 1)));
        await _client.PatchAsync($"/api/orders/{matching.OrderId}/paid", null);
        await _client.PatchAsync($"/api/orders/{wrongStatus.OrderId}/cancel", null);
        await SetCreatedAtAsync(matching.OrderId, new DateTime(2026, 9, 15, 12, 30, 0, DateTimeKind.Utc));
        await SetCreatedAtAsync(wrongStatus.OrderId, new DateTime(2026, 9, 15, 12, 30, 0, DateTimeKind.Utc));
        await SetCreatedAtAsync(wrongName.OrderId, new DateTime(2026, 9, 15, 12, 30, 0, DateTimeKind.Utc));

        var orders = await _client.GetFromJsonAsync<List<OrderDto>>(
            "/api/orders?status=Paid&search=Table&from=2026-09-15&to=2026-09-15");

        Assert.Equal(matching.OrderId, Assert.Single(orders!).OrderId);
    }

    [Fact]
    public async Task GetOrders_SearchMatchesOrderNumber()
    {
        var product = await AddProductAsync("Drinks", "Tea", 2.40m);
        var expected = await CreateOrderAsync(CreateRequest("Lunch", (product.ProductId, 1)));

        var orders = await _client.GetFromJsonAsync<List<OrderDto>>($"/api/orders?search={expected.OrderId}");

        Assert.Equal(expected.OrderId, Assert.Single(orders!).OrderId);
    }

    [Fact]
    public async Task GetOrder_UnknownIdReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/orders/2147483647");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal("Resource not found", problem?.Title);
    }

    [Fact]
    public async Task GetOrders_InvalidDateRangeReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/orders?from=2026-09-30&to=2026-09-01");

        await AssertValidationProblemAsync(response, "From");
    }

    [Fact]
    public async Task GetOrders_ConflictingStatusAndLegacyFilterReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/orders?status=Paid&isPaid=false");

        await AssertValidationProblemAsync(response, "Status");
    }

    private async Task<Product> AddProductAsync(string categoryName, string name, decimal unitPrice, bool isActive = true)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TillAppDbContext>();
        var category = await dbContext.Categories.SingleOrDefaultAsync(candidate => candidate.Name == categoryName);
        if (category is null)
        {
            category = new Category { Name = categoryName };
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync();
        }

        var product = new Product
        {
            CategoryId = category.CategoryId,
            Name = name,
            UnitPrice = unitPrice,
            IsActive = isActive
        };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }

    private async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private async Task SetCreatedAtAsync(int orderId, DateTime createdAt)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TillAppDbContext>();
        var order = await dbContext.Orders.SingleAsync(candidate => candidate.OrderId == orderId);
        order.CreatedAt = createdAt;
        await dbContext.SaveChangesAsync();
    }

    private static CreateOrderRequest CreateRequest(string orderName, params (int ProductId, int Quantity)[] items) => new()
    {
        OrderName = orderName,
        Items = items
            .Select(item => new CreateOrderItemRequest { ProductId = item.ProductId, Quantity = item.Quantity })
            .ToList()
    };

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string errorKey)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.Contains(errorKey, problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ProblemResponse>());
    }

    private sealed record ValidationProblemResponse(Dictionary<string, string[]> Errors);

    private sealed record ProblemResponse(string? Title, string? Detail, int? Status);
}
