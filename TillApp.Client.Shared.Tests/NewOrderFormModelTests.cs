using System.ComponentModel.DataAnnotations;
using TillApp.Client.Shared.Models;
using TillApp.Shared.Catalog;

namespace TillApp.Client.Shared.Tests;

public sealed class NewOrderFormModelTests
{
    [Fact]
    public void AddProduct_RepeatedProductIncrementsTheExistingLine()
    {
        var model = new NewOrderFormModel();
        var product = Product(1, "Burger", 5.00m);

        model.AddProduct(product);
        model.AddProduct(product);

        var line = Assert.Single(model.Items);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(10.00m, model.Total);
    }

    [Fact]
    public void QuantityActions_UpdatePreviewAndRemoveAtOne()
    {
        var model = new NewOrderFormModel();
        model.AddProduct(Product(1, "Burger", 5.00m));
        model.Increment(1);

        model.Decrement(1);
        Assert.Equal(1, model.Items[0].Quantity);

        model.Decrement(1);
        Assert.Empty(model.Items);
        Assert.Equal(0m, model.Total);
    }

    [Fact]
    public void Clear_RemovesCartButPreservesOrderName()
    {
        var model = new NewOrderFormModel { OrderName = "Table 4" };
        model.AddProduct(Product(1, "Burger", 5.00m));

        model.Clear();

        Assert.Empty(model.Items);
        Assert.Equal("Table 4", model.OrderName);
    }

    [Fact]
    public void ToCreateOrderRequest_UsesProductIdsAndQuantitiesOnly()
    {
        var model = new NewOrderFormModel { OrderName = "Takeaway" };
        model.AddProduct(Product(2, "Tea", 2.40m));
        model.AddProduct(Product(2, "Tea", 2.40m));

        var request = model.ToCreateOrderRequest();

        Assert.Equal("Takeaway", request.OrderName);
        var item = Assert.Single(request.Items!);
        Assert.Equal(2, item.ProductId);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(0m, item.Price);
        Assert.Equal(string.Empty, item.ItemName);
    }

    [Fact]
    public void Validation_RequiresNameAndAtLeastOneProduct()
    {
        var model = new NewOrderFormModel();
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.OrderName)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Items)));
    }

    private static ProductDto Product(int id, string name, decimal price) =>
        new(id, 1, "Food", name, price, true);
}
