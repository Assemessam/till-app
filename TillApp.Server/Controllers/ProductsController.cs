using Microsoft.AspNetCore.Mvc;
using TillApp.Server.Services;
using TillApp.Shared.Catalog;

namespace TillApp.Server.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(IProductCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(
        [FromQuery] int? categoryId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var products = await catalogService.GetProductsAsync(categoryId, isActive, cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(int id, CancellationToken cancellationToken)
    {
        var product = await catalogService.GetProductAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
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
        try
        {
            var product = await catalogService.CreateProductAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
        }
        catch (ProductCatalogException exception)
        {
            return CatalogProblem(exception);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(
        int id,
        ProductRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var product = await catalogService.UpdateProductAsync(id, request, cancellationToken);
            return product is null ? NotFound() : Ok(product);
        }
        catch (ProductCatalogException exception)
        {
            return CatalogProblem(exception);
        }
    }

    [HttpPatch("{id:int}/active")]
    [ProducesResponseType<ProductDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> SetProductActive(
        int id,
        SetProductActiveRequest request,
        CancellationToken cancellationToken)
    {
        var product = await catalogService.SetProductActiveAsync(id, request.IsActive, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    private ObjectResult CatalogProblem(ProductCatalogException exception) =>
        exception.Error == ProductCatalogError.NotFound
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Catalog resource not found",
                Detail = exception.Message
            })
            : Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Duplicate product name",
                Detail = exception.Message
            });
}
