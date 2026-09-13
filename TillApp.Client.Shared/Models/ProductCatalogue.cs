namespace TillApp.Client.Shared.Models;

public static class ProductCatalogue
{
    public static IReadOnlyList<Product> All { get; } = Array.AsReadOnly<Product>(
    [
        new("Coke", 2.20m),
        new("Water", 1.50m),
        new("Coffee", 2.80m),
        new("Tea", 2.40m),
        new("Burger", 7.50m),
        new("Cheeseburger", 8.50m),
        new("Fries", 3.25m),
        new("Pizza", 9.00m),
        new("Salad", 6.30m),
        new("Ice Cream", 3.75m)
    ]);
}
