using TillApp.Shared.Catalog;

namespace TillApp.Server.Services;

public interface IProductCatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken);

    Task<CategoryDto?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken);

    Task<CategoryDto> CreateCategoryAsync(CategoryRequest request, CancellationToken cancellationToken);

    Task<CategoryDto?> UpdateCategoryAsync(int categoryId, CategoryRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        int? categoryId,
        bool? isActive,
        CancellationToken cancellationToken);

    Task<ProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken);

    Task<ProductDto> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken);

    Task<ProductDto?> UpdateProductAsync(int productId, ProductRequest request, CancellationToken cancellationToken);

    Task<ProductDto?> SetProductActiveAsync(
        int productId,
        bool isActive,
        CancellationToken cancellationToken);
}
