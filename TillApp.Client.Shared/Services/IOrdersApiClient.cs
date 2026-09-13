using TillApp.Shared.Orders;

namespace TillApp.Client.Shared.Services;

public interface IOrdersApiClient
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetUnpaidOrdersAsync(CancellationToken cancellationToken = default);

    Task<OrderDto> MarkOrderPaidAsync(int orderId, CancellationToken cancellationToken = default);
}
