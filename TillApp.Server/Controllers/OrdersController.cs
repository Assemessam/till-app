using Microsoft.AspNetCore.Mvc;
using TillApp.Server.Services;
using TillApp.Shared.Orders;

namespace TillApp.Server.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders(
        [FromQuery] OrderStatus? status,
        [FromQuery] bool? isPaid,
        [FromQuery] string? search,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var orders = await orderService.GetOrdersAsync(status, isPaid, search, from, to, cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetOrderAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderService.CreateOrderAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
        }
        catch (OrderDomainException exception)
        {
            return OrderProblem(exception);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> UpdateOrder(
        int id,
        UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderService.UpdateOrderAsync(id, request, cancellationToken);
            return order is null ? NotFound() : Ok(order);
        }
        catch (OrderDomainException exception)
        {
            return OrderProblem(exception);
        }
    }

    [HttpPatch("{id:int}/paid")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> MarkOrderPaid(int id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderService.MarkOrderPaidAsync(id, cancellationToken);
            return order is null ? NotFound() : Ok(order);
        }
        catch (OrderDomainException exception)
        {
            return OrderProblem(exception);
        }
    }

    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> CancelOrder(int id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await orderService.CancelOrderAsync(id, cancellationToken);
            return order is null ? NotFound() : Ok(order);
        }
        catch (OrderDomainException exception)
        {
            return OrderProblem(exception);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await orderService.DeleteOrderAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (OrderDomainException exception)
        {
            return OrderProblem(exception);
        }
    }

    private ObjectResult OrderProblem(OrderDomainException exception) =>
        exception.Error is OrderDomainError.InvalidTransition or OrderDomainError.OrderNotPending
            ? Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid order status transition",
                Detail = exception.Message
            })
            : BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Order cannot be created or updated",
                Detail = exception.Message
            });
}
