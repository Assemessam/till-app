using Microsoft.EntityFrameworkCore;
using TillApp.Server.Data;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Orders;

namespace TillApp.Server.Services;

public sealed class OrderService(
    TillAppDbContext dbContext,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
        bool? isPaid,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .AsQueryable();

        if (isPaid.HasValue)
        {
            query = query.Where(order => order.IsPaid == isPaid.Value);
        }

        var orders = await query
            .OrderBy(order => order.OrderId)
            .ToListAsync(cancellationToken);

        return orders.Select(ToDto).ToList();
    }

    public async Task<OrderDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogDebug("Order {OrderId} was not found", orderId);
        }

        return order is null ? null : ToDto(order);
    }

    public async Task<OrderDto> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = new Order
        {
            OrderName = request.OrderName,
            Amount = CalculateAmount(request.Items!),
            IsPaid = false,
            Items = CreateItems(request.Items!)
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created order {OrderId} with {ItemCount} items and amount {Amount}",
            order.OrderId,
            order.Items.Count,
            order.Amount);

        return ToDto(order);
    }

    public async Task<OrderDto?> UpdateOrderAsync(
        int orderId,
        UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Could not update order {OrderId} because it does not exist", orderId);
            return null;
        }

        order.OrderName = request.OrderName;
        order.Amount = CalculateAmount(request.Items!);

        dbContext.OrderItems.RemoveRange(order.Items);
        order.Items = CreateItems(request.Items!);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated order {OrderId} with {ItemCount} replacement items and amount {Amount}",
            order.OrderId,
            order.Items.Count,
            order.Amount);

        return ToDto(order);
    }

    public async Task<OrderDto?> MarkOrderPaidAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Could not mark order {OrderId} paid because it does not exist", orderId);
            return null;
        }

        if (!order.IsPaid)
        {
            order.IsPaid = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Marked order {OrderId} as paid", orderId);
        }
        else
        {
            logger.LogDebug("Order {OrderId} was already paid", orderId);
        }

        return ToDto(order);
    }

    public async Task<bool> DeleteOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Could not delete order {OrderId} because it does not exist", orderId);
            return false;
        }

        dbContext.Orders.Remove(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted order {OrderId}", orderId);
        return true;
    }

    private static decimal CalculateAmount(IEnumerable<CreateOrderItemRequest> items) =>
        items.Sum(item => item.Price);

    private static List<OrderItem> CreateItems(IEnumerable<CreateOrderItemRequest> items) =>
        items.Select(item => new OrderItem
        {
            ItemName = item.ItemName,
            Price = item.Price
        }).ToList();

    private static OrderDto ToDto(Order order) =>
        new(
            order.OrderId,
            order.OrderName,
            order.Amount,
            order.IsPaid,
            order.Items
                .OrderBy(item => item.OrderItemId)
                .Select(item => new OrderItemDto(item.OrderItemId, item.ItemName, item.Price))
                .ToList());
}
