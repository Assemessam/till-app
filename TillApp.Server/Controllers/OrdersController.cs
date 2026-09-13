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
        [FromQuery] bool? isPaid,
        CancellationToken cancellationToken)
    {
        var orders = await orderService.GetOrdersAsync(isPaid, cancellationToken);
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
        var order = await orderService.CreateOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, order);
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
        var order = await orderService.UpdateOrderAsync(id, request, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPatch("{id:int}/paid")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> MarkOrderPaid(int id, CancellationToken cancellationToken)
    {
        var order = await orderService.MarkOrderPaidAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(int id, CancellationToken cancellationToken)
    {
        var deleted = await orderService.DeleteOrderAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
