using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TillApp.Server.Infrastructure;
using TillApp.Server.Services;
using TillApp.Shared.Catalog;

namespace TillApp.Server.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(
        [FromQuery, Range(1, int.MaxValue)] int? categoryId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var products = await catalogService.GetProductsAsync(categoryId, isActive, cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.GetProductAsync(id, cancellationToken);
        return product is null ? ApiProblems.NotFound(HttpContext, "product") : Ok(product);
    }

    [HttpPost]
    [ProducesResponseType<ProductDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        [Range(1, int.MaxValue)] int id,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.UpdateProductAsync(id, request, cancellationToken);
        return product is null ? ApiProblems.NotFound(HttpContext, "product") : Ok(product);
    }

    [HttpPatch("{id:int}/active")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> SetProductActive(
        [Range(1, int.MaxValue)] int id,
        SetProductActiveRequest request,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.SetProductActiveAsync(id, request.IsActive, cancellationToken);
        return product is null ? ApiProblems.NotFound(HttpContext, "product") : Ok(product);
    }
}
