using System.Net.Http.Json;
using System.Text.Json;
using TillApp.Shared.Orders;

namespace TillApp.Client.Shared.Services;

public sealed class OrdersApiClient(HttpClient httpClient) : IOrdersApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        using var response = await SendAsync(
            () => httpClient.GetAsync("api/orders?isPaid=false", cancellationToken),
            cancellationToken);

        return await ReadRequiredAsync<List<OrderDto>>(response, cancellationToken);
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
