using System.ComponentModel.DataAnnotations;

namespace TillApp.Shared.Orders;

public sealed class CreateOrderItemRequest
{
    private string _itemName = string.Empty;

    [Range(0, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    [StringLength(100)]
    public string ItemName
    {
        get => _itemName;
        set => _itemName = value?.Trim() ?? string.Empty;
    }

    public decimal Price { get; set; }
}
