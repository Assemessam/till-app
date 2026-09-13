using Microsoft.Extensions.Logging;
using TillApp.Client.Shared.Services;

namespace TillApp.Client.MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        var apiBaseUrl = Environment.GetEnvironmentVariable("TILLAPP_API_BASE_URL")
            ?? "http://10.0.2.2:5080/";

        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl, UriKind.Absolute)
        });
        builder.Services.AddTillAppClient();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
