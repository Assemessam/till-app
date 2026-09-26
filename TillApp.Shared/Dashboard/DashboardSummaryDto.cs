namespace TillApp.Shared.Dashboard;

public sealed record DashboardSummaryDto(
    int TodayOrders,
    int PendingOrders,
    int PaidOrders,
    int CancelledOrders,
    decimal TodayRevenue,
    IReadOnlyList<RecentOrderDto> RecentOrders);
