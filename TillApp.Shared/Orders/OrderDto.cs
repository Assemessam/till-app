namespace TillApp.Shared.Orders;

public sealed record OrderDto(
    int OrderId,
    string OrderName,
    decimal Amount,
    bool IsPaid,
    IReadOnlyList<OrderItemDto> Items);
