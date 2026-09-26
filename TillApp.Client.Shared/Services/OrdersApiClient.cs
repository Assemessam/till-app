using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using TillApp.Shared.Orders;
using TillApp.Shared.Catalog;
using TillApp.Shared.Dashboard;

namespace TillApp.Client.Shared.Services;

public sealed class OrdersApiClient(HttpClient httpClient) : IOrdersApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<DashboardSummaryDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync("api/dashboard", cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<DashboardSummaryDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync("api/categories", cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<List<CategoryDto>>(response, cancellationToken);
    }

    public async Task<CategoryDto> CreateCategoryAsync(
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.PostAsJsonAsync("api/categories", request, cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<CategoryDto>(response, cancellationToken);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(
        int categoryId,
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.PutAsJsonAsync($"api/categories/{categoryId}", request, cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<CategoryDto>(response, cancellationToken);
    }

    public async Task DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.DeleteAsync($"api/categories/{categoryId}", cancellationToken),
            cancellationToken);
    }

    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(
        int? categoryId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (categoryId.HasValue)
        {
            query.Add($"categoryId={categoryId.Value}");
        }

        if (isActive.HasValue)
        {
            query.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        }

        var path = "api/products" + (query.Count > 0 ? $"?{string.Join('&', query)}" : string.Empty);
        using var response = await SendAsync(() => httpClient.GetAsync(path, cancellationToken), cancellationToken);
        return await ReadRequiredAsync<List<ProductDto>>(response, cancellationToken);
    }

    public async Task<ProductDto> CreateProductAsync(
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.PostAsJsonAsync("api/products", request, cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<ProductDto>(response, cancellationToken);
    }

    public async Task<ProductDto> UpdateProductAsync(
        int productId,
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.PutAsJsonAsync($"api/products/{productId}", request, cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<ProductDto>(response, cancellationToken);
    }

    public async Task<ProductDto> SetProductActiveAsync(
        int productId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/products/{productId}/active")
        {
            Content = JsonContent.Create(new SetProductActiveRequest(isActive))
        };
        using var response = await SendAsync(() => httpClient.SendAsync(request, cancellationToken), cancellationToken);
        return await ReadRequiredAsync<ProductDto>(response, cancellationToken);
    }

    public async Task<OrderDto> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.PostAsJsonAsync("api/orders", request, cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<OrderDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderDto>> GetUnpaidOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetOrdersAsync(OrderStatus.Pending, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(
        OrderStatus? status = null,
        string? search = null,
        DateOnly? from = null,
        DateOnly? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (status.HasValue)
        {
            query.Add($"status={status.Value}");
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        }

        if (from.HasValue)
        {
            query.Add($"from={from.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }

        if (to.HasValue)
        {
            query.Add($"to={to.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }

        var path = "api/orders" + (query.Count > 0 ? $"?{string.Join('&', query)}" : string.Empty);
        using var response = await SendAsync(
            () => httpClient.GetAsync(path, cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<List<OrderDto>>(response, cancellationToken);
    }

    public async Task<OrderDto> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => httpClient.GetAsync($"api/orders/{orderId}", cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<OrderDto>(response, cancellationToken);
    }

    public async Task<OrderDto> MarkOrderPaidAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/orders/{orderId}/paid");
        using var response = await SendAsync(
            () => httpClient.SendAsync(request, cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<OrderDto>(response, cancellationToken);
    }

    public async Task<OrderDto> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/orders/{orderId}/cancel");
        using var response = await SendAsync(
            () => httpClient.SendAsync(request, cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<OrderDto>(response, cancellationToken);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;

        try
        {
            response = await send();
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OrdersApiException(
                "The TillApp service took too long to respond. Please try again.",
                innerException: exception);
        }
        catch (HttpRequestException exception)
        {
            throw new OrdersApiException(
                "The TillApp service is unavailable. Check that the API is running and try again.",
                innerException: exception);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var problem = await TryReadProblemAsync(response, cancellationToken);
        var statusCode = response.StatusCode;
        response.Dispose();

        throw new OrdersApiException(
            problem?.Detail ?? problem?.Title ?? "The request could not be completed. Please try again.",
            statusCode,
            problem?.Errors);
    }

    private static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return value ?? throw new OrdersApiException("The TillApp service returned an empty response.");
        }
        catch (JsonException exception)
        {
            throw new OrdersApiException(
                "The TillApp service returned an unexpected response. Please try again.",
                innerException: exception);
        }
        catch (NotSupportedException exception)
        {
            throw new OrdersApiException(
                "The TillApp service returned an unsupported response. Please try again.",
                innerException: exception);
        }
    }

    private static async Task<ApiProblem?> TryReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ApiProblem>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private sealed record ApiProblem(
        string? Title,
        string? Detail,
        Dictionary<string, string[]>? Errors);
}
