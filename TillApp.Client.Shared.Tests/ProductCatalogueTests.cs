using System.ComponentModel.DataAnnotations;
using TillApp.Client.Shared.Models;

namespace TillApp.Client.Shared.Tests;

public sealed class ProductCatalogueTests
{
    [Fact]
    public void Catalogue_HasTenUniquePositivePricedProducts()
    {
        Assert.Equal(10, ProductCatalogue.All.Count);
        Assert.Equal(10, ProductCatalogue.All.Select(product => product.Name).Distinct().Count());
        Assert.All(ProductCatalogue.All, product => Assert.True(product.Price > 0));
    }

    [Fact]
    public void BrowserLunchProducts_TotalTwelvePoundsNinetyFive()
    {
        var selectedNames = new[] { "Coke", "Burger", "Fries" };
        var total = ProductCatalogue.All
            .Where(product => selectedNames.Contains(product.Name))
            .Sum(product => product.Price);

        Assert.Equal(12.95m, total);
    }

    [Fact]
    public void NewOrder_BlankNameAndNoItems_AreInvalid()
    {
        var model = new NewOrderFormModel();
        var results = Validate(model);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.OrderName)));
        Assert.Contains(results, result => result.MemberNames.Contains(nameof(model.Items)));
    }

    [Fact]
    public void NewOrder_NameAndSelectedProduct_AreValid()
    {
        var model = new NewOrderFormModel { OrderName = "Browser Lunch" };
        model.Items.Add(ProductCatalogue.All.Single(product => product.Name == "Coke"));

        Assert.Empty(Validate(model));
    }

    private static List<ValidationResult> Validate(NewOrderFormModel model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }
}
