namespace TillApp.Server.Services;

public enum OrderDomainError
{
    ProductNotFound,
    ProductInactive,
    InvalidTransition,
    OrderNotPending
}

public sealed class OrderDomainException(OrderDomainError error, string message) : Exception(message)
{
    public OrderDomainError Error { get; } = error;
}
