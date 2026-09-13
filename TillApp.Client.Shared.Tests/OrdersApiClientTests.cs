using System.Net;
using System.Net.Http.Json;
using TillApp.Client.Shared.Services;
using TillApp.Shared.Orders;

namespace TillApp.Client.Shared.Tests;

public sealed class OrdersApiClientTests
{
    [Fact]
    public async Task GetUnpaidOrders_UsesServerSidePaymentFilter()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/orders?isPaid=false", request.RequestUri?.PathAndQuery);
            return JsonResponse(new[] { SampleOrder() });
        });

        var client = CreateClient(handler);
        var orders = await client.GetUnpaidOrdersAsync();

        Assert.Single(orders);
        Assert.False(orders[0].IsPaid);
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreatedOrder()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/orders", request.RequestUri?.AbsolutePath);
            return JsonResponse(SampleOrder(), HttpStatusCode.Created);
        });
        var client = CreateClient(handler);

        var order = await client.CreateOrderAsync(new CreateOrderRequest
        {
            OrderName = "Browser Lunch",
            Items = [new CreateOrderItemRequest { ItemName = "Coke", Price = 2.20m }]
        });

        Assert.Equal(42, order.OrderId);
    }

    [Fact]
    public async Task MarkPaid_UsesPatchEndpoint()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Patch, request.Method);
            Assert.Equal("/api/orders/42/paid", request.RequestUri?.AbsolutePath);
            return JsonResponse(SampleOrder() with { IsPaid = true });
        });
        var client = CreateClient(handler);

        var order = await client.MarkOrderPaidAsync(42);

        Assert.True(order.IsPaid);
    }

    [Fact]
    public async Task ValidationProblem_IsConvertedToSafeFieldErrors()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(
            new
            {
                title = "One or more validation errors occurred.",
                status = 400,
                errors = new Dictionary<string, string[]>
                {
                    ["OrderName"] = ["Enter an order name."]
                }
            },
            HttpStatusCode.BadRequest));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<OrdersApiException>(() =>
            client.CreateOrderAsync(new CreateOrderRequest()));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("Enter an order name.", exception.ValidationErrors["OrderName"][0]);
        Assert.DoesNotContain("{", exception.Message);
    }

    [Fact]
    public async Task NetworkFailure_IsConvertedToRecoverableMessage()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("Internal socket detail"));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<OrdersApiException>(() => client.GetUnpaidOrdersAsync());

        Assert.Contains("unavailable", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("socket", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static OrdersApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5080/") });

    private static HttpResponseMessage JsonResponse<T>(T value, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = JsonContent.Create(value) };

    private static OrderDto SampleOrder() => new(
        42,
        "Browser Lunch",
        2.20m,
        false,
        [new OrderItemDto(100, "Coke", 2.20m)]);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
