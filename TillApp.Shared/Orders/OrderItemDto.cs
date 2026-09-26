namespace TillApp.Shared.Orders;

public sealed record OrderItemDto(
    int OrderItemId,
    int? ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;

    public string ItemName => ProductName;

    public decimal Price => UnitPrice;
}
