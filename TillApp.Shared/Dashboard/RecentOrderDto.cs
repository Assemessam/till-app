using TillApp.Shared.Orders;

namespace TillApp.Shared.Dashboard;

public sealed record RecentOrderDto(
    int OrderId,
    string OrderName,
    decimal Amount,
    OrderStatus Status,
    DateTime CreatedAt);
