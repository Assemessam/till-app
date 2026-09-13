namespace TillApp.Server.Data.Entities;

public sealed class Order
{
    public int OrderId { get; set; }

    public string OrderName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public bool IsPaid { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
