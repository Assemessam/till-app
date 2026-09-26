using System.ComponentModel.DataAnnotations;

namespace TillApp.Shared.Orders;

public sealed class CreateOrderRequest : IValidatableObject
{
    private string _orderName = string.Empty;

    [Required]
    [StringLength(100)]
    public string OrderName
    {
        get => _orderName;
        set => _orderName = value?.Trim() ?? string.Empty;
    }

    [Required]
    [MinLength(1)]
    public List<CreateOrderItemRequest>? Items { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Items?.Any(item => item is null) == true)
        {
            yield return new ValidationResult("Order items cannot be null.", [nameof(Items)]);
            yield break;
        }

        if (Items is not null && Items.Any(item => item.ProductId == 0 && string.IsNullOrWhiteSpace(item.ItemName)))
        {
            yield return new ValidationResult(
                "Each order item must specify a product.",
                [nameof(Items)]);
        }
    }
}
