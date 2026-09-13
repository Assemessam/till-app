using System.ComponentModel.DataAnnotations;
using TillApp.Shared.Common;

namespace TillApp.Shared.Orders;

public sealed class CreateOrderItemRequest
{
    private string _itemName = string.Empty;

    [Required]
    [StringLength(100)]
    public string ItemName
    {
        get => _itemName;
        set => _itemName = value?.Trim() ?? string.Empty;
    }

    [SqlMoney]
    public decimal Price { get; set; }
}
