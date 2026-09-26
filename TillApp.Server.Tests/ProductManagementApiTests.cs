using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TillApp.Server.Data;
using TillApp.Shared.Catalog;

namespace TillApp.Server.Tests;

[Collection(SqlServerApiCollection.Name)]
public sealed class ProductManagementApiTests(OrderApiFactory factory) : IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TillAppDbContext>();
        await dbContext.Database.MigrateAsync();
        await dbContext.Products.ExecuteDeleteAsync();
        await dbContext.Categories.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Categories_CreateReturnsLocationAndTrimmedName()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new CategoryRequest { Name = "  Drinks  " });
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(category);
        Assert.True(category.CategoryId > 0);
        Assert.Equal("Drinks", category.Name);
    }

    [Fact]
    public async Task Categories_ListReturnsSortedCategories()
    {
        await CreateCategoryAsync("Snacks");
        await CreateCategoryAsync("Drinks");

        var categories = await _client.GetFromJsonAsync<List<CategoryDto>>("/api/categories");

        Assert.Equal(["Drinks", "Snacks"], categories!.Select(category => category.Name));
    }

    [Fact]
    public async Task Categories_GetUnknownReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/categories/2147483647");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Categories_DuplicateNameReturnsConflictProblem()
    {
        await CreateCategoryAsync("Drinks");

        var response = await _client.PostAsJsonAsync("/api/categories", new CategoryRequest { Name = "Drinks" });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Categories_WhitespaceNameReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new CategoryRequest { Name = "   " });

        await AssertValidationProblemAsync(response, "Name");
    }

    [Fact]
    public async Task Categories_UpdateRenamesCategory()
    {
        var created = await CreateCategoryAsync("Drinks");

        var response = await _client.PutAsJsonAsync(
            $"/api/categories/{created.CategoryId}",
            new CategoryRequest { Name = "Cold Drinks" });
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Cold Drinks", updated!.Name);
    }

    [Fact]
    public async Task Categories_DeleteEmptyCategorySucceeds()
    {
        var category = await CreateCategoryAsync("Drinks");

        var response = await _client.DeleteAsync($"/api/categories/{category.CategoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/categories/{category.CategoryId}")).StatusCode);
    }

    [Fact]
    public async Task Categories_DeleteWithProductsReturnsConflict()
    {
        var category = await CreateCategoryAsync("Drinks");
        await CreateProductAsync(category.CategoryId, "Tea", 2.40m);

        var response = await _client.DeleteAsync($"/api/categories/{category.CategoryId}");

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Products_CreateReturnsCategoryAndProductDetails()
    {
        var category = await CreateCategoryAsync("Drinks");

        var response = await _client.PostAsJsonAsync("/api/products", ProductRequest(category.CategoryId, "Tea", 2.40m));
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.NotNull(product);
        Assert.True(product.ProductId > 0);
        Assert.Equal(category.CategoryId, product.CategoryId);
        Assert.Equal("Drinks", product.CategoryName);
        Assert.Equal("Tea", product.Name);
        Assert.Equal(2.40m, product.UnitPrice);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task Products_CreateUnknownCategoryReturnsNotFoundProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/products", ProductRequest(2147483647, "Tea", 2.40m));

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Products_InvalidPriceReturnsValidationProblem()
    {
        var category = await CreateCategoryAsync("Drinks");
        var response = await _client.PostAsJsonAsync("/api/products", ProductRequest(category.CategoryId, "Tea", 0m));

        await AssertValidationProblemAsync(response, "UnitPrice");
    }

    [Fact]
    public async Task Products_DuplicateNameWithinCategoryReturnsConflict()
    {
        var category = await CreateCategoryAsync("Drinks");
        await CreateProductAsync(category.CategoryId, "Tea", 2.40m);

        var response = await _client.PostAsJsonAsync("/api/products", ProductRequest(category.CategoryId, "Tea", 2.50m));

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Products_SameNameCanExistInDifferentCategories()
    {
        var first = await CreateCategoryAsync("Drinks");
        var second = await CreateCategoryAsync("Desserts");
        await CreateProductAsync(first.CategoryId, "Tea", 2.40m);

        var response = await _client.PostAsJsonAsync("/api/products", ProductRequest(second.CategoryId, "Tea", 2.40m));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Products_ListSupportsCategoryAndActiveFilters()
    {
        var drinks = await CreateCategoryAsync("Drinks");
        var desserts = await CreateCategoryAsync("Desserts");
        var tea = await CreateProductAsync(drinks.CategoryId, "Tea", 2.40m);
        await CreateProductAsync(drinks.CategoryId, "Water", 1.50m);
        await CreateProductAsync(desserts.CategoryId, "Cake", 3.00m);
        await _client.PatchAsJsonAsync($"/api/products/{tea.ProductId}/active", new SetProductActiveRequest(false));

        var active = await _client.GetFromJsonAsync<List<ProductDto>>($"/api/products?categoryId={drinks.CategoryId}&isActive=true");
        var inactive = await _client.GetFromJsonAsync<List<ProductDto>>($"/api/products?categoryId={drinks.CategoryId}&isActive=false");

        var activeProduct = Assert.Single(active!);
        Assert.Equal("Water", activeProduct.Name);
        var inactiveProduct = Assert.Single(inactive!);
        Assert.Equal("Tea", inactiveProduct.Name);
    }

    [Fact]
    public async Task Products_UpdateChangesCategoryNameAndPrice()
    {
        var drinks = await CreateCategoryAsync("Drinks");
        var desserts = await CreateCategoryAsync("Desserts");
        var product = await CreateProductAsync(drinks.CategoryId, "Tea", 2.40m);

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{product.ProductId}",
            ProductRequest(desserts.CategoryId, "Tea Cake", 3.50m));
        var updated = await response.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(desserts.CategoryId, updated!.CategoryId);
        Assert.Equal("Desserts", updated.CategoryName);
        Assert.Equal("Tea Cake", updated.Name);
        Assert.Equal(3.50m, updated.UnitPrice);
    }

    [Fact]
    public async Task Products_SetActiveCanReactivateProduct()
    {
        var category = await CreateCategoryAsync("Drinks");
        var product = await CreateProductAsync(category.CategoryId, "Tea", 2.40m);

        var response = await _client.PatchAsJsonAsync(
            $"/api/products/{product.ProductId}/active",
            new SetProductActiveRequest(false));
        var inactive = await response.Content.ReadFromJsonAsync<ProductDto>();
        var reactivateResponse = await _client.PatchAsJsonAsync(
            $"/api/products/{product.ProductId}/active",
            new SetProductActiveRequest(true));
        var reactivated = await reactivateResponse.Content.ReadFromJsonAsync<ProductDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(inactive!.IsActive);
        Assert.True(reactivated!.IsActive);
    }

    [Fact]
    public async Task Products_GetUnknownReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/products/2147483647");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<CategoryDto> CreateCategoryAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new CategoryRequest { Name = name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>())!;
    }

    private async Task<ProductDto> CreateProductAsync(int categoryId, string name, decimal unitPrice)
    {
        var response = await _client.PostAsJsonAsync("/api/products", ProductRequest(categoryId, name, unitPrice));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductDto>())!;
    }

    private static ProductRequest ProductRequest(int categoryId, string name, decimal unitPrice) => new()
    {
        CategoryId = categoryId,
        Name = name,
        UnitPrice = unitPrice
    };

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, string errorKey)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.Contains(errorKey, problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ProblemResponse>());
    }

    private sealed record ValidationProblemResponse(Dictionary<string, string[]> Errors);

    private sealed record ProblemResponse(string? Title, string? Detail, int? Status);
}
