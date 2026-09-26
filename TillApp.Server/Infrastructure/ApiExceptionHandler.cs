using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TillApp.Server.Services;

namespace TillApp.Server.Infrastructure;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ProductCatalogException catalogException => CreateCatalogProblem(catalogException),
            OrderDomainException orderException => CreateOrderProblem(orderException),
            _ => CreateUnexpectedProblem(httpContext, exception)
        };

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        });
    }

    private static ProblemDetails CreateCatalogProblem(ProductCatalogException exception) =>
        exception.Error switch
        {
            ProductCatalogError.NotFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Catalog resource not found",
                Detail = exception.Message
            },
            ProductCatalogError.CategoryHasProducts => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Category contains products",
                Detail = exception.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate catalog name",
                Detail = exception.Message
            }
        };

    private static ProblemDetails CreateOrderProblem(OrderDomainException exception) =>
        exception.Error is OrderDomainError.InvalidTransition or OrderDomainError.OrderNotPending
            ? new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid order status transition",
                Detail = exception.Message
            }
            : new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Order cannot be created or updated",
                Detail = exception.Message
            };

    private ProblemDetails CreateUnexpectedProblem(HttpContext httpContext, Exception exception)
    {
        logger.LogError(
            exception,
            "Unhandled API exception for {Method} {Path} with trace {TraceIdentifier}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The request could not be completed. Please try again."
        };
    }
}
