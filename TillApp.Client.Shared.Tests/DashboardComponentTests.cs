using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TillApp.Client.Shared.Pages;
using TillApp.Client.Shared.Services;
using TillApp.Shared.Dashboard;
using TillApp.Shared.Orders;

namespace TillApp.Client.Shared.Tests;

public sealed class DashboardComponentTests
{
    [Fact]
    public async Task Dashboard_RendersMetricValuesAndRecentOrderDetailsLink()
    {
        var summary = new DashboardSummaryDto(
            12,
            3,
            7,
            2,
            126.50m,
            [new RecentOrderDto(42, "Table 4", 18.50m, OrderStatus.Paid, DateTime.UtcNow)]);
        using var handler = new DashboardHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(summary)
        }));
        await using var provider = CreateServices(handler);
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<Dashboard>(ParameterView.Empty);
            return component.ToHtmlString();
        });
        html = WebUtility.HtmlDecode(html);

        Assert.Contains("Today’s Orders", html);
        Assert.Contains("<strong>12</strong>", html);
        Assert.Contains("<strong>3</strong>", html);
        Assert.Contains("<strong>7</strong>", html);
        Assert.Contains("<strong>2</strong>", html);
        Assert.Contains("£126.50", html);
        Assert.Contains("Table 4", html);
        Assert.Contains("href=\"/orders/42\"", html);
        Assert.Contains("Paid", html);
    }

    [Fact]
    public async Task Dashboard_ShowsFriendlyErrorAndRetryActionWhenApiFails()
    {
        using var handler = new DashboardHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        await using var provider = CreateServices(handler);
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<Dashboard>(ParameterView.Empty);
            return component.ToHtmlString();
        });

        Assert.Contains("We couldn’t load the dashboard.", html);
        Assert.Contains("The request could not be completed", html);
        Assert.Contains("Retry", html);
        Assert.DoesNotContain("stack", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_ShowsLoadingStateUntilApiResponseArrives()
    {
        var response = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new DashboardHandler(_ => response.Task);
        await using var provider = CreateServices(handler);
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var root = await renderer.Dispatcher.InvokeAsync(() => renderer.BeginRenderingComponent<Dashboard>());
        var loadingHtml = await renderer.Dispatcher.InvokeAsync(root.ToHtmlString);
        Assert.Contains("Loading dashboard…", loadingHtml);
        Assert.DoesNotContain("<strong>0</strong>", loadingHtml);

        response.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new DashboardSummaryDto(4, 1, 2, 1, 20m, []))
        });
        await root.QuiescenceTask;

        var loadedHtml = await renderer.Dispatcher.InvokeAsync(root.ToHtmlString);
        Assert.Contains("Today’s Orders", WebUtility.HtmlDecode(loadedHtml));
    }

    private static ServiceProvider CreateServices(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton<IOrdersApiClient>(
            new OrdersApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }));
        return services.BuildServiceProvider();
    }

    private sealed class DashboardHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> createResponse)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => createResponse(request);
    }
}
