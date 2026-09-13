namespace TillApp.Server.Data.Entities;

public sealed class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public Order Order { get; set; } = null!;
}
