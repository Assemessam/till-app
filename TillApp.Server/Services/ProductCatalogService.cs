using Microsoft.EntityFrameworkCore;
using TillApp.Server.Data;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Catalog;

namespace TillApp.Server.Services;

public sealed class ProductCatalogService(
    TillAppDbContext dbContext,
    ILogger<ProductCatalogService> logger) : IProductCatalogService
{
    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryDto(category.CategoryId, category.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.CategoryId == categoryId)
            .Select(category => new CategoryDto(category.CategoryId, category.Name))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CategoryDto> CreateCategoryAsync(
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureCategoryNameAvailableAsync(request.Name, null, cancellationToken);

        var category = new Category { Name = request.Name };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created category {CategoryId}", category.CategoryId);

        return new CategoryDto(category.CategoryId, category.Name);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(
        int categoryId,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(candidate => candidate.CategoryId == categoryId, cancellationToken);

        if (category is null)
        {
            return null;
        }

        await EnsureCategoryNameAvailableAsync(request.Name, categoryId, cancellationToken);
        category.Name = request.Name;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated category {CategoryId}", category.CategoryId);

        return new CategoryDto(category.CategoryId, category.Name);
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .SingleOrDefaultAsync(candidate => candidate.CategoryId == categoryId, cancellationToken);

        if (category is null)
        {
            return false;
        }

        if (await dbContext.Products.AnyAsync(product => product.CategoryId == categoryId, cancellationToken))
        {
            throw new ProductCatalogException(
                ProductCatalogError.CategoryHasProducts,
                "A category cannot be deleted while it contains products.");
        }

        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted category {CategoryId}", categoryId);
        return true;
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        int? categoryId,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking().AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(product => product.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(product => product.Name)
            .Select(product => new ProductDto(
                product.ProductId,
                product.CategoryId,
                product.Category.Name,
                product.Name,
                product.UnitPrice,
                product.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.ProductId == productId)
            .Select(product => new ProductDto(
                product.ProductId,
                product.CategoryId,
                product.Category.Name,
                product.Name,
                product.UnitPrice,
                product.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDto> CreateProductAsync(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories.AnyAsync(category => category.CategoryId == request.CategoryId, cancellationToken))
        {
            throw new ProductCatalogException(ProductCatalogError.NotFound, "The selected category was not found.");
        }

        await EnsureProductNameAvailableAsync(request.CategoryId, request.Name, null, cancellationToken);

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            UnitPrice = request.UnitPrice,
            IsActive = true
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created product {ProductId} in category {CategoryId}", product.ProductId, product.CategoryId);

        return await GetProductAsync(product.ProductId, cancellationToken)
            ?? throw new InvalidOperationException("The newly created product could not be reloaded.");
    }

    public async Task<ProductDto?> UpdateProductAsync(
        int productId,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(candidate => candidate.ProductId == productId, cancellationToken);

        if (product is null)
        {
            return null;
        }

        if (!await dbContext.Categories.AnyAsync(category => category.CategoryId == request.CategoryId, cancellationToken))
        {
            throw new ProductCatalogException(ProductCatalogError.NotFound, "The selected category was not found.");
        }

        await EnsureProductNameAvailableAsync(request.CategoryId, request.Name, productId, cancellationToken);
        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.UnitPrice = request.UnitPrice;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated product {ProductId}", product.ProductId);

        return await GetProductAsync(product.ProductId, cancellationToken);
    }

    public async Task<ProductDto?> SetProductActiveAsync(
        int productId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .SingleOrDefaultAsync(candidate => candidate.ProductId == productId, cancellationToken);

        if (product is null)
        {
            return null;
        }

        if (product.IsActive != isActive)
        {
            product.IsActive = isActive;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Set product {ProductId} active state to {IsActive}", product.ProductId, isActive);
        }

        return await GetProductAsync(product.ProductId, cancellationToken);
    }

    private async Task EnsureCategoryNameAvailableAsync(
        string name,
        int? exceptCategoryId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Categories.AnyAsync(
                category => category.Name == name && category.CategoryId != exceptCategoryId,
                cancellationToken))
        {
            throw new ProductCatalogException(ProductCatalogError.DuplicateName, "A category with this name already exists.");
        }
    }

    private async Task EnsureProductNameAvailableAsync(
        int categoryId,
        string name,
        int? exceptProductId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(
                product => product.CategoryId == categoryId
                    && product.Name == name
                    && product.ProductId != exceptProductId,
                cancellationToken))
        {
            throw new ProductCatalogException(
                ProductCatalogError.DuplicateName,
                "A product with this name already exists in the selected category.");
        }
    }
}
