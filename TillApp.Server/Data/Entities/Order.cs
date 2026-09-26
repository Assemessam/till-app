using TillApp.Shared.Orders;

namespace TillApp.Server.Data.Entities;

public sealed class Order
{
    public int OrderId { get; set; }

    public string OrderName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
