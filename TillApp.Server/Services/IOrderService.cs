using TillApp.Shared.Orders;

namespace TillApp.Server.Services;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetOrdersAsync(bool? isPaid, CancellationToken cancellationToken);

    Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken);

    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken);

    Task<OrderDto?> UpdateOrderAsync(int orderId, UpdateOrderRequest request, CancellationToken cancellationToken);

    Task<OrderDto?> MarkOrderPaidAsync(int orderId, CancellationToken cancellationToken);

    Task<bool> DeleteOrderAsync(int orderId, CancellationToken cancellationToken);
}
