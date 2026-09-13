using System.Net;

namespace TillApp.Client.Shared.Services;

public sealed class OrdersApiException : Exception
{
    public OrdersApiException(
        string message,
        HttpStatusCode? statusCode = null,
        IReadOnlyDictionary<string, string[]>? validationErrors = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ValidationErrors = validationErrors ?? new Dictionary<string, string[]>();
    }

    public HttpStatusCode? StatusCode { get; }

    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }
}
