using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TillApp.Server.Tests;

public sealed class OrderApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = Environment.GetEnvironmentVariable("TILLAPP_TEST_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "TILLAPP_TEST_CONNECTION_STRING must target the disposable TillAppTests SQL Server database.");

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TillApp"] = connectionString
            });
        });
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerApiCollection : ICollectionFixture<OrderApiFactory>
{
    public const string Name = "SQL Server API";
}
