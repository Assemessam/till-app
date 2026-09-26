using TillApp.Shared.Orders;
using TillApp.Shared.Catalog;
using TillApp.Shared.Dashboard;

namespace TillApp.Client.Shared.Services;

public interface IOrdersApiClient
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateCategoryAsync(CategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateCategoryAsync(int categoryId, CategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        int? categoryId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<ProductDto> CreateProductAsync(ProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateProductAsync(int productId, ProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> SetProductActiveAsync(
        int productId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetUnpaidOrdersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
        OrderStatus? status = null,
        string? search = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default);

    Task<OrderDto> GetOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderDto> MarkOrderPaidAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderDto> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default);
}
