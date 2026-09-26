using System.ComponentModel.DataAnnotations;
using TillApp.Shared.Catalog;
using TillApp.Shared.Orders;

namespace TillApp.Client.Shared.Models;

public sealed class NewOrderFormModel
{
    [Required(ErrorMessage = "Enter an order name.")]
    [StringLength(100, ErrorMessage = "Order name cannot exceed 100 characters.")]
    public string OrderName { get; set; } = string.Empty;

    [MinLength(1, ErrorMessage = "Select at least one product.")]
    public List<PosOrderLine> Items { get; } = [];

    public decimal Total => Items.Sum(item => item.LineTotal);

    public int ItemCount => Items.Sum(item => item.Quantity);

    public void AddProduct(ProductDto product)
    {
        var existingItem = Items.SingleOrDefault(item => item.ProductId == product.ProductId);
        if (existingItem is not null)
        {
            existingItem.Quantity++;
            return;
        }

        Items.Add(new PosOrderLine(product.ProductId, product.Name, product.UnitPrice));
    }

    public void Increment(int productId) =>
        Items.Single(item => item.ProductId == productId).Quantity++;

    public void Decrement(int productId)
    {
        var item = Items.Single(item => item.ProductId == productId);
        if (item.Quantity == 1)
        {
            Items.Remove(item);
            return;
        }

        item.Quantity--;
    }

    public void Remove(int productId) =>
        Items.RemoveAll(item => item.ProductId == productId);

    public void Clear() => Items.Clear();

    public CreateOrderRequest ToCreateOrderRequest() => new()
    {
        OrderName = OrderName,
        Items = Items
            .Select(item => new CreateOrderItemRequest
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            })
            .ToList()
    };
}

public sealed class PosOrderLine(int productId, string productName, decimal unitPrice)
{
    public int ProductId { get; } = productId;

    public string ProductName { get; } = productName;

    public decimal UnitPrice { get; } = unitPrice;

    public int Quantity { get; set; } = 1;

    public decimal LineTotal => UnitPrice * Quantity;
}
