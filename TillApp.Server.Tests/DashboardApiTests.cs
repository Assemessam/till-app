using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TillApp.Server.Data;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Dashboard;
using TillApp.Shared.Orders;

namespace TillApp.Server.Tests;

[Collection(SqlServerApiCollection.Name)]
public sealed class DashboardApiTests(OrderApiFactory factory) : IAsyncLifetime
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
    public async Task Dashboard_ReturnsUtcMetricsPaidRevenueAndEightNewestOrders()
    {
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var rows = new List<Order>
        {
            CreateOrder("Created at start", 1m, OrderStatus.Pending, today),
            CreateOrder("Pending today", 100m, OrderStatus.Pending, today.AddHours(1)),
            CreateOrder("Paid today", 10m, OrderStatus.Paid, today.AddHours(2), today.AddHours(3)),
            CreateOrder("Paid at start", 5m, OrderStatus.Paid, today.AddHours(2), today),
            CreateOrder("Cancelled today", 30m, OrderStatus.Cancelled, today.AddHours(4), cancelledAt: today.AddHours(5)),
            CreateOrder("Paid yesterday", 20m, OrderStatus.Paid, today.AddDays(-1).AddHours(22), today.AddDays(-1).AddHours(23)),
            CreateOrder("Paid tomorrow", 7m, OrderStatus.Paid, today.AddDays(1), today.AddDays(1)),
            CreateOrder("Cancelled yesterday", 40m, OrderStatus.Cancelled, today.AddDays(-1).AddHours(21), cancelledAt: today.AddDays(-1).AddHours(22)),
            CreateOrder("Pending yesterday", 50m, OrderStatus.Pending, today.AddDays(-1).AddHours(20))
        };
        rows.AddRange(Enumerable.Range(1, 8).Select(index =>
            CreateOrder($"Older {index}", index, OrderStatus.Paid, today.AddDays(-2).AddHours(index), today.AddDays(-2).AddHours(index + 1))));

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TillAppDbContext>();
            dbContext.Orders.AddRange(rows);
            await dbContext.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/dashboard");
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(summary);
        Assert.Equal(5, summary.TodayOrders);
        Assert.Equal(3, summary.PendingOrders);
        Assert.Equal(2, summary.PaidOrders);
        Assert.Equal(1, summary.CancelledOrders);
        Assert.Equal(15m, summary.TodayRevenue);
        Assert.Equal(8, summary.RecentOrders.Count);
        Assert.Equal("Paid tomorrow", summary.RecentOrders[0].OrderName);
        Assert.Equal("Cancelled today", summary.RecentOrders[1].OrderName);
        Assert.Equal("Paid at start", summary.RecentOrders[2].OrderName);
    }

    private static Order CreateOrder(
        string name,
        decimal amount,
        OrderStatus status,
        DateTime createdAt,
        DateTime? paidAt = null,
        DateTime? cancelledAt = null) => new()
    {
        OrderName = name,
        Amount = amount,
        Status = status,
        CreatedAt = createdAt,
        PaidAt = paidAt,
        CancelledAt = cancelledAt
    };
}
