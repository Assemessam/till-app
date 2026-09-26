using System.Net;
using System.Net.Http.Json;
using TillApp.Client.Shared.Services;
using TillApp.Shared.Catalog;

namespace TillApp.Client.Shared.Tests;

public sealed class CatalogApiClientTests
{
    [Fact]
    public async Task GetProducts_UsesCategoryAndActiveFilters()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/products?categoryId=4&isActive=false", request.RequestUri?.PathAndQuery);
            return JsonResponse(new[] { SampleProduct() with { IsActive = false } });
        });
        var client = CreateClient(handler);

        var products = await client.GetProductsAsync(4, false);

        Assert.Single(products);
        Assert.False(products[0].IsActive);
    }

    [Fact]
    public async Task CreateCategory_PostsSharedCategoryRequest()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/categories", request.RequestUri?.AbsolutePath);
            return JsonResponse(new CategoryDto(8, "Drinks"), HttpStatusCode.Created);
        });
        var client = CreateClient(handler);

        var category = await client.CreateCategoryAsync(new CategoryRequest { Name = "Drinks" });

        Assert.Equal(8, category.CategoryId);
    }

    [Fact]
    public async Task DeleteCategory_AcceptsNoContentResponse()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/api/categories/8", request.RequestUri?.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = CreateClient(handler);

        await client.DeleteCategoryAsync(8);
    }

    [Fact]
    public async Task ConflictProblem_IsConvertedToSafeMessage()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse(
            new { title = "Category contains products", detail = "A category cannot be deleted while it contains products." },
            HttpStatusCode.Conflict));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<OrdersApiException>(() => client.DeleteCategoryAsync(8));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Contains("cannot be deleted", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{", exception.Message);
    }

    private static OrdersApiClient CreateClient(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5080/") });

    private static HttpResponseMessage JsonResponse<T>(T value, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = JsonContent.Create(value) };

    private static ProductDto SampleProduct() => new(12, 4, "Drinks", "Tea", 2.40m, true);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
