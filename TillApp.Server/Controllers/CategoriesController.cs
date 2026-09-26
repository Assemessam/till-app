using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TillApp.Server.Infrastructure;
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
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategory(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        var category = await catalogService.GetCategoryAsync(id, cancellationToken);
        return category is null ? ApiProblems.NotFound(HttpContext, "category") : Ok(category);
    }

    [HttpPost]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await catalogService.CreateCategoryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<CategoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(
        [Range(1, int.MaxValue)] int id,
        CategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = await catalogService.UpdateCategoryAsync(id, request, cancellationToken);
        return category is null ? ApiProblems.NotFound(HttpContext, "category") : Ok(category);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(
        [Range(1, int.MaxValue)] int id,
        CancellationToken cancellationToken)
    {
        return await catalogService.DeleteCategoryAsync(id, cancellationToken)
            ? NoContent()
            : ApiProblems.NotFound(HttpContext, "category");
    }
}
