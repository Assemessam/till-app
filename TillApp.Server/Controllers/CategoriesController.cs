using Microsoft.AspNetCore.Mvc;
using TillApp.Server.Services;
using TillApp.Shared.Catalog;

namespace TillApp.Server.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(IProductCatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategoryDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        return Ok(await catalogService.GetCategoriesAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id, CancellationToken cancellationToken)
    {
        var category = await catalogService.GetCategoryAsync(id, cancellationToken);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await catalogService.CreateCategoryAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
        }
        catch (ProductCatalogException exception)
        {
            return CatalogProblem(exception);
        }
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(
        int id,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var category = await catalogService.UpdateCategoryAsync(id, request, cancellationToken);
            return category is null ? NotFound() : Ok(category);
        }
        catch (ProductCatalogException exception)
        {
            return CatalogProblem(exception);
        }
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken cancellationToken)
    {
        try
        {
            return await catalogService.DeleteCategoryAsync(id, cancellationToken)
                ? NoContent()
                : NotFound();
        }
        catch (ProductCatalogException exception)
        {
            return CatalogProblem(exception);
        }
    }

    private ObjectResult CatalogProblem(ProductCatalogException exception) =>
        exception.Error == ProductCatalogError.NotFound
            ? NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Category not found",
                Detail = exception.Message
            })
            : Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = exception.Error == ProductCatalogError.CategoryHasProducts
                    ? "Category contains products"
                    : "Duplicate category name",
                Detail = exception.Message
            });
}
