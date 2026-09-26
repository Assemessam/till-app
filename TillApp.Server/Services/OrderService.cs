using Microsoft.EntityFrameworkCore;
using TillApp.Server.Data;
using TillApp.Server.Data.Entities;
using TillApp.Shared.Common;
using TillApp.Shared.Orders;

namespace TillApp.Server.Services;

public sealed class OrderService(
    TillAppDbContext dbContext,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
        OrderStatus? status,
        bool? isPaid,
        string? search,
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .AsQueryable();

        var effectiveStatus = status ?? isPaid switch
        {
            true => OrderStatus.Paid,
            false => OrderStatus.Pending,
            null => null
        };

        if (effectiveStatus.HasValue)
        {
            query = query.Where(order => order.Status == effectiveStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (int.TryParse(term, out var orderId))
            {
                query = query.Where(order => order.OrderId == orderId || order.OrderName.Contains(term));
            }
            else
            {
                query = query.Where(order => order.OrderName.Contains(term));
            }
        }

        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            query = query.Where(order => order.CreatedAt >= fromUtc);
        }

        if (to.HasValue && to.Value != DateOnly.MaxValue)
        {
            var toExclusive = DateTime.SpecifyKind(
                to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc);
            query = query.Where(order => order.CreatedAt < toExclusive);
        }

        var orders = await query
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.OrderId)
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
        var items = await CreateItemsAsync(request.Items!, cancellationToken);
        var order = new Order
        {
            OrderName = request.OrderName,
            Amount = CalculateAmount(items),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Items = items
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created pending order {OrderId} with {ItemCount} lines and total {Amount}",
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

        EnsurePending(order, "updated");
        var items = await CreateItemsAsync(request.Items!, cancellationToken);

        order.OrderName = request.OrderName;
        order.Amount = CalculateAmount(items);
        dbContext.OrderItems.RemoveRange(order.Items);
        order.Items = items;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated pending order {OrderId} with {ItemCount} lines and total {Amount}",
            order.OrderId,
            order.Items.Count,
            order.Amount);

        return ToDto(order);
    }

    public async Task<OrderDto?> MarkOrderPaidAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await FindOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new OrderDomainException(
                OrderDomainError.InvalidTransition,
                "Cancelled orders cannot be marked paid.");
        }

        if (order.Status == OrderStatus.Pending)
        {
            order.Status = OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Marked order {OrderId} as paid", orderId);
        }

        return ToDto(order);
    }

    public async Task<OrderDto?> CancelOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await FindOrderAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.Status == OrderStatus.Paid)
        {
            throw new OrderDomainException(
                OrderDomainError.InvalidTransition,
                "Paid orders cannot be cancelled.");
        }

        if (order.Status == OrderStatus.Pending)
        {
            order.Status = OrderStatus.Cancelled;
            order.CancelledAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Cancelled order {OrderId}", orderId);
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

        EnsurePending(order, "deleted");
        dbContext.Orders.Remove(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted pending order {OrderId}", orderId);
        return true;
    }

    private async Task<Order?> FindOrderAsync(int orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(candidate => candidate.OrderId == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} was not found", orderId);
        }

        return order;
    }

    private async Task<List<OrderItem>> CreateItemsAsync(
        IEnumerable<CreateOrderItemRequest> requests,
        CancellationToken cancellationToken)
    {
        var resolvedItems = new List<(Product Product, int Quantity)>();

        foreach (var request in requests)
        {
            var product = await ResolveProductAsync(request, cancellationToken);
            resolvedItems.Add((product, request.Quantity));
        }

        var items = resolvedItems
            .GroupBy(item => item.Product.ProductId)
            .Select(group =>
            {
                var product = group.First().Product;
                var quantity = group.Sum(item => (long)item.Quantity);
                if (quantity > int.MaxValue)
                {
                    throw new OrderDomainException(
                        OrderDomainError.ProductNotFound,
                        "The requested quantity is too large.");
                }

                return new OrderItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    UnitPrice = product.UnitPrice,
                    Quantity = (int)quantity
                };
            })
            .ToList();

        return items;
    }

    private async Task<Product> ResolveProductAsync(
        CreateOrderItemRequest request,
        CancellationToken cancellationToken)
    {
        Product? product;

        if (request.ProductId > 0)
        {
            product = await dbContext.Products
                .SingleOrDefaultAsync(candidate => candidate.ProductId == request.ProductId, cancellationToken);
        }
        else
        {
            var matches = await dbContext.Products
                .Where(candidate => candidate.Name == request.ItemName)
                .Take(2)
                .ToListAsync(cancellationToken);

            product = matches.Count == 1 ? matches[0] : null;
        }

        if (product is null)
        {
            throw new OrderDomainException(
                OrderDomainError.ProductNotFound,
                "One or more selected products could not be found.");
        }

        if (!product.IsActive)
        {
            throw new OrderDomainException(
                OrderDomainError.ProductInactive,
                $"{product.Name} is inactive and cannot be added to a new order.");
        }

        return product;
    }

    private static decimal CalculateAmount(IEnumerable<OrderItem> items)
    {
        var total = items.Sum(item => item.UnitPrice * item.Quantity);
        if (total > SqlMoney.MaxValue)
        {
            throw new OrderDomainException(
                OrderDomainError.ProductNotFound,
                "The order total exceeds the SQL Server money range.");
        }

        return total;
    }

    private static void EnsurePending(Order order, string action)
    {
        if (order.Status != OrderStatus.Pending)
        {
            throw new OrderDomainException(
                OrderDomainError.OrderNotPending,
                $"Only pending orders can be {action}.");
        }
    }

    private static OrderDto ToDto(Order order) =>
        new(
            order.OrderId,
            order.OrderName,
            order.Amount,
            order.Status,
            order.CreatedAt,
            order.PaidAt,
            order.CancelledAt,
            order.Items
                .OrderBy(item => item.OrderItemId)
                .Select(item => new OrderItemDto(
                    item.OrderItemId,
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity))
                .ToList());
}
