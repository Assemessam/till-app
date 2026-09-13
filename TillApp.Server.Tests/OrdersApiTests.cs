using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TillApp.Server.Data;
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
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_ValidOrder_ReturnsCreatedWithGeneratedIds()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", ValidCreateRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.True(order.OrderId > 0);
        Assert.All(order.Items, item => Assert.True(item.OrderItemId > 0));
    }

    [Fact]
    public async Task Create_CalculatesAmountFromItems()
    {
        var order = await CreateOrderAsync(ValidCreateRequest());

        Assert.Equal(12.95m, order.Amount);
    }

    [Fact]
    public async Task Create_IgnoresClientSuppliedPaymentAndAmountFields()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            orderName = "Attempted override",
            amount = 0.01m,
            isPaid = true,
            orderId = 999,
            items = new[]
            {
                new { orderItemId = 999, itemName = "Coke", price = 2.20m }
            }
        });

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(order);
        Assert.Equal(2.20m, order.Amount);
        Assert.False(order.IsPaid);
        Assert.NotEqual(999, order.OrderId);
        Assert.NotEqual(999, order.Items[0].OrderItemId);
    }

    [Fact]
    public async Task Create_EmptyItemList_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            orderName = "Empty",
            items = Array.Empty<object>()
        });

        await AssertValidationProblemAsync(response, "Items");
    }

    [Fact]
    public async Task Create_MissingOrderName_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { itemName = "Coke", price = 2.20m } }
        });

        await AssertValidationProblemAsync(response, "OrderName");
    }

    [Fact]
    public async Task Create_OverlongOrderName_ReturnsValidationProblem()
    {
        var request = ValidCreateRequest();
        request.OrderName = new string('x', 101);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        await AssertValidationProblemAsync(response, "OrderName");
    }

    [Fact]
    public async Task Create_InvalidItemName_ReturnsValidationProblem()
    {
        var request = ValidCreateRequest();
        request.Items![0].ItemName = "   ";

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        await AssertValidationProblemAsync(response, "Items[0].ItemName");
    }

    [Fact]
    public async Task Create_OverlongItemName_ReturnsValidationProblem()
    {
        var request = ValidCreateRequest();
        request.Items![0].ItemName = new string('x', 101);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        await AssertValidationProblemAsync(response, "Items[0].ItemName");
    }

    [Fact]
    public async Task Create_NullItem_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            orderName = "Invalid",
            items = new object?[] { null }
        });

        await AssertValidationProblemAsync(response, "Items");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.00001")]
    [InlineData("922337203685477.5808")]
    public async Task Create_InvalidPrice_ReturnsValidationProblem(string value)
    {
        var request = ValidCreateRequest();
        request.Items![0].Price = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        await AssertValidationProblemAsync(response, "Items[0].Price");
    }

    [Fact]
    public async Task Get_ReturnsPersistedOrderAndItems()
    {
        var created = await CreateOrderAsync(ValidCreateRequest());

        var response = await _client.GetAsync($"/api/orders/{created.OrderId}");
        var retrieved = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(retrieved);
        Assert.Equal(created.OrderId, retrieved.OrderId);
        Assert.Equal(created.OrderName, retrieved.OrderName);
        Assert.Equal(created.Amount, retrieved.Amount);
        Assert.Equal(created.IsPaid, retrieved.IsPaid);
        Assert.Equal(created.Items, retrieved.Items);
    }

    [Fact]
    public async Task Get_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/orders/2147483647");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrders_UnpaidFilterReturnsOnlyUnpaidOrders()
    {
        var unpaid = await CreateOrderAsync(ValidCreateRequest("Unpaid"));
        var paid = await CreateOrderAsync(ValidCreateRequest("Paid"));
        await _client.PatchAsync($"/api/orders/{paid.OrderId}/paid", null);

        var orders = await _client.GetFromJsonAsync<List<OrderDto>>("/api/orders?isPaid=false");

        var order = Assert.Single(orders!);
        Assert.Equal(unpaid.OrderId, order.OrderId);
        Assert.False(order.IsPaid);
    }

    [Fact]
    public async Task GetOrders_PaidOrderDisappearsFromUnpaidResults()
    {
        var order = await CreateOrderAsync(ValidCreateRequest());

        var before = await _client.GetFromJsonAsync<List<OrderDto>>("/api/orders?isPaid=false");
        await _client.PatchAsync($"/api/orders/{order.OrderId}/paid", null);
        var after = await _client.GetFromJsonAsync<List<OrderDto>>("/api/orders?isPaid=false");

        Assert.Contains(before!, candidate => candidate.OrderId == order.OrderId);
        Assert.DoesNotContain(after!, candidate => candidate.OrderId == order.OrderId);
    }

    [Fact]
    public async Task Update_ReplacesItemsRecalculatesAmountAndPreservesPaymentState()
    {
        var created = await CreateOrderAsync(ValidCreateRequest());
        await _client.PatchAsync($"/api/orders/{created.OrderId}/paid", null);
        var request = new UpdateOrderRequest
        {
            OrderName = "Updated lunch",
            Items =
            [
                new CreateOrderItemRequest { ItemName = "Tea", Price = 1.50m },
                new CreateOrderItemRequest { ItemName = "Cake", Price = 4.25m }
            ]
        };

        var response = await _client.PutAsJsonAsync($"/api/orders/{created.OrderId}", request);
        var updated = await response.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Updated lunch", updated.OrderName);
        Assert.Equal(5.75m, updated.Amount);
        Assert.True(updated.IsPaid);
        Assert.Equal(["Tea", "Cake"], updated.Items.Select(item => item.ItemName));
    }

    [Fact]
    public async Task Update_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync("/api/orders/2147483647", ValidUpdateRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkPaid_SetsPaymentFlagAndIsIdempotent()
    {
        var created = await CreateOrderAsync(ValidCreateRequest());

        var firstResponse = await _client.PatchAsync($"/api/orders/{created.OrderId}/paid", null);
        var secondResponse = await _client.PatchAsync($"/api/orders/{created.OrderId}/paid", null);
        var order = await secondResponse.Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.NotNull(order);
        Assert.True(order.IsPaid);
    }

    [Fact]
    public async Task MarkPaid_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.PatchAsync("/api/orders/2147483647/paid", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesOrderAndCascadeDeletesItems()
    {
        var created = await CreateOrderAsync(ValidCreateRequest());

        var response = await _client.DeleteAsync($"/api/orders/{created.OrderId}");
        var subsequentGet = await _client.GetAsync($"/api/orders/{created.OrderId}");

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TillAppDbContext>();
        var itemCount = await dbContext.OrderItems.CountAsync(item => item.OrderId == created.OrderId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, subsequentGet.StatusCode);
        Assert.Equal(0, itemCount);
    }

    [Fact]
    public async Task Delete_UnknownOrder_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync("/api/orders/2147483647");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/orders", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private static CreateOrderRequest ValidCreateRequest(string orderName = "Test Lunch") => new()
    {
        OrderName = orderName,
        Items =
        [
            new CreateOrderItemRequest { ItemName = "Coke", Price = 2.20m },
            new CreateOrderItemRequest { ItemName = "Burger", Price = 7.50m },
            new CreateOrderItemRequest { ItemName = "Fries", Price = 3.25m }
        ]
    };

    private static UpdateOrderRequest ValidUpdateRequest() => new()
    {
        OrderName = "Updated",
        Items = [new CreateOrderItemRequest { ItemName = "Tea", Price = 1.50m }]
    };

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string errorKey)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.Contains(errorKey, problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    private sealed record ValidationProblemResponse(Dictionary<string, string[]> Errors);
}
