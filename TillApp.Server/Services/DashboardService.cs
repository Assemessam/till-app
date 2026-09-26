using Microsoft.EntityFrameworkCore;
using TillApp.Server.Data;
using TillApp.Shared.Dashboard;
using TillApp.Shared.Orders;

namespace TillApp.Server.Services;

public sealed class DashboardService(TillAppDbContext dbContext) : IDashboardService
{
    private const int RecentOrderLimit = 8;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var utcStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var utcEnd = utcStart.AddDays(1);

        var todayOrders = await dbContext.Orders.CountAsync(
            order => order.CreatedAt >= utcStart && order.CreatedAt < utcEnd,
            cancellationToken);
        var pendingOrders = await dbContext.Orders.CountAsync(
            order => order.Status == OrderStatus.Pending,
            cancellationToken);
        var paidToday = dbContext.Orders.Where(order =>
            order.Status == OrderStatus.Paid &&
            order.PaidAt >= utcStart && order.PaidAt < utcEnd);
        var cancelledOrders = await dbContext.Orders.CountAsync(order =>
            order.Status == OrderStatus.Cancelled &&
            order.CancelledAt >= utcStart && order.CancelledAt < utcEnd,
            cancellationToken);
        var paidOrders = await paidToday.CountAsync(cancellationToken);
        var todayRevenue = await paidToday
            .Select(order => (decimal?)order.Amount)
            .SumAsync(cancellationToken) ?? 0m;
        var recentOrders = await dbContext.Orders
            .AsNoTracking()
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.OrderId)
            .Take(RecentOrderLimit)
            .Select(order => new RecentOrderDto(
                order.OrderId,
                order.OrderName,
                order.Amount,
                order.Status,
                order.CreatedAt))
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto(
            todayOrders,
            pendingOrders,
            paidOrders,
            cancelledOrders,
            todayRevenue,
            recentOrders);
    }
}
