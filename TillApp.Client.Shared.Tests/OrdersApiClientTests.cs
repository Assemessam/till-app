using System.Net;
using System.Net.Http.Json;
using TillApp.Client.Shared.Services;
using TillApp.Shared.Orders;
using TillApp.Shared.Dashboard;

namespace TillApp.Client.Shared.Tests;

public sealed class OrdersApiClientTests
{
    [Fact]
    public async Task GetUnpaidOrders_UsesServerSidePendingFilter()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/orders?status=Pending", request.RequestUri?.PathAndQuery);
            return JsonResponse(new[] { SampleOrder() });
        });

        var client = CreateClient(handler);
        var orders = await client.GetUnpaidOrdersAsync();

        Assert.Single(orders);
        Assert.False(orders[0].IsPaid);
    }

    [Fact]
    public async Task GetOrders_SendsCombinedStatusSearchAndInclusiveDateFilters()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "/api/orders?status=Paid&search=Table%204%2F%231&from=2026-09-01&to=2026-09-30",
                request.RequestUri?.PathAndQuery);
            return JsonResponse(Array.Empty<OrderDto>());
        });
        var client = CreateClient(handler);

        var orders = await client.GetOrdersAsync(
            OrderStatus.Paid,
            " Table 4/#1 ",
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 9, 30));

        Assert.Empty(orders);
    }

    [Fact]
    public async Task GetOrder_UsesDetailsEndpoint()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/orders/42", request.RequestUri?.AbsolutePath);
            return JsonResponse(SampleOrder());
        });
        var client = CreateClient(handler);

        var order = await client.GetOrderAsync(42);

        Assert.Equal(42, order.OrderId);
        var item = Assert.Single(order.Items);
        Assert.Equal("Coke", item.ProductName);
        Assert.Equal(2.20m, item.UnitPrice);
        Assert.Equal(1, item.Quantity);
        Assert.Equal(2.20m, item.LineTotal);
    }

    [Fact]
    public async Task CancelOrder_UsesPendingCancellationTransitionEndpoint()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Patch, request.Method);
            Assert.Equal("/api/orders/42/cancel", request.RequestUri?.AbsolutePath);
            return JsonResponse(SampleOrder() with { Status = OrderStatus.Cancelled });
        });
        var client = CreateClient(handler);

        var order = await client.CancelOrderAsync(42);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public async Task GetDashboard_UsesDashboardEndpointAndReturnsMetricsAndRecentOrders()
    {
        var expected = new DashboardSummaryDto(
            12,
            3,
            7,
            2,
            126.50m,
            [new RecentOrderDto(42, "Table 4", 18.50m, OrderStatus.Paid, DateTime.UtcNow)]);
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/dashboard", request.RequestUri?.AbsolutePath);
            return JsonResponse(expected);
        });
        var client = CreateClient(handler);

        var summary = await client.GetDashboardAsync();

        Assert.Equal(12, summary.TodayOrders);
        Assert.Equal(3, summary.PendingOrders);
        Assert.Equal(7, summary.PaidOrders);
        Assert.Equal(2, summary.CancelledOrders);
        Assert.Equal(126.50m, summary.TodayRevenue);
        var recent = Assert.Single(summary.RecentOrders);
        Assert.Equal(42, recent.OrderId);
        Assert.Equal("Table 4", recent.OrderName);
        Assert.Equal(OrderStatus.Paid, recent.Status);
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
            return JsonResponse(SampleOrder() with { Status = OrderStatus.Paid });
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
        OrderStatus.Pending,
        DateTime.UtcNow,
        null,
        null,
        [new OrderItemDto(100, 5, "Coke", 2.20m, 1)]);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
