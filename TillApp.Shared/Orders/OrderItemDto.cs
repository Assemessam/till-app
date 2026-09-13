namespace TillApp.Shared.Orders;

public sealed record OrderItemDto(
    int OrderItemId,
    string ItemName,
    decimal Price);
