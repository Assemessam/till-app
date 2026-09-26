using Microsoft.EntityFrameworkCore;
using TillApp.Server.Data.Entities;

namespace TillApp.Server.Data;

public static class DevelopmentDataSeeder
{
    private static readonly (string Category, string Name, decimal UnitPrice)[] SampleProducts =
    [
        ("Food", "Burger", 8.50m),
        ("Food", "Pizza", 9.50m),
        ("Food", "Fries", 3.00m),
        ("Drinks", "Coffee", 2.75m),
        ("Drinks", "Coke", 2.25m),
        ("Drinks", "Water", 1.50m),
        ("Desserts", "Cake", 4.25m)
    ];

    public static async Task SeedAsync(TillAppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var categoryNames = SampleProducts.Select(product => product.Category).Distinct().ToArray();
        var existingCategories = await dbContext.Categories
            .Where(category => categoryNames.Contains(category.Name))
            .ToListAsync(cancellationToken);
        var categories = existingCategories.ToDictionary(category => category.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var name in categoryNames)
        {
            if (categories.ContainsKey(name))
            {
                continue;
            }

            var category = new Category { Name = name };
            dbContext.Categories.Add(category);
            categories.Add(name, category);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var (categoryName, name, unitPrice) in SampleProducts)
        {
            var category = categories[categoryName];
            var productExists = await dbContext.Products.AnyAsync(
                product => product.CategoryId == category.CategoryId && product.Name == name,
                cancellationToken);

            if (!productExists)
            {
                dbContext.Products.Add(new Product
                {
                    CategoryId = category.CategoryId,
                    Name = name,
                    UnitPrice = unitPrice,
                    IsActive = true
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
