using System.ComponentModel.DataAnnotations;

namespace TillApp.Client.Shared.Models;

public sealed class NewOrderFormModel
{
    [Required(ErrorMessage = "Enter an order name.")]
    [StringLength(100, ErrorMessage = "Order name cannot exceed 100 characters.")]
    public string OrderName { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Select at least one product.")]
    public List<Product> Items { get; } = [];
}
