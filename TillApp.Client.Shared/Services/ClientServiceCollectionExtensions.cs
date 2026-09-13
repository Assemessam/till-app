using Microsoft.Extensions.DependencyInjection;

namespace TillApp.Client.Shared.Services;

public static class ClientServiceCollectionExtensions
{
    public static IServiceCollection AddTillAppClient(this IServiceCollection services)
    {
        services.AddScoped<IOrdersApiClient, OrdersApiClient>();
        return services;
    }
}
