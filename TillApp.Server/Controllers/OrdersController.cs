using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TillApp.Server.Infrastructure;
using TillApp.Server.Services;
using TillApp.Shared.Orders;

namespace TillApp.Server.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<OrderDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetOrders(
        [FromQuery] OrderQuery query,
        CancellationToken cancellationToken)
    {
        var orders = await orderService.GetOrdersAsync(query, cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var order = await orderService.GetOrderAsync(id, cancellationToken);
        return order is null ? ApiProblems.NotFound(HttpContext, "order") : Ok(order);
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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> UpdateOrder(
        [Range(1, int.MaxValue)] int id,
        UpdateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var order = await orderService.UpdateOrderAsync(id, request, cancellationToken);
        return order is null ? ApiProblems.NotFound(HttpContext, "order") : Ok(order);
    }

    [HttpPatch("{id:int}/paid")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> MarkOrderPaid(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var order = await orderService.MarkOrderPaidAsync(id, cancellationToken);
        return order is null ? ApiProblems.NotFound(HttpContext, "order") : Ok(order);
    }

    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> CancelOrder(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var order = await orderService.CancelOrderAsync(id, cancellationToken);
        return order is null ? ApiProblems.NotFound(HttpContext, "order") : Ok(order);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteOrder(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var deleted = await orderService.DeleteOrderAsync(id, cancellationToken);
        return deleted ? NoContent() : ApiProblems.NotFound(HttpContext, "order");
    }
}
