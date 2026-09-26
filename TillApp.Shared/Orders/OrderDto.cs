namespace TillApp.Shared.Orders;

public sealed record OrderDto(
    int OrderId,
    string OrderName,
    decimal Amount,
    OrderStatus Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? CancelledAt,
    IReadOnlyList<OrderItemDto> Items)
{
    public bool IsPaid => Status == OrderStatus.Paid;
}
