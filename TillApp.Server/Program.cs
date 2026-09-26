using Microsoft.EntityFrameworkCore;
using TillApp.Server.Infrastructure;
using TillApp.Server.Data;
using TillApp.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<TillAppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("TillApp")
        ?? throw new InvalidOperationException("Connection string 'TillApp' is required.");

    options.UseSqlServer(connectionString);
});
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();

const string WasmDevelopmentCorsPolicy = "WasmDevelopment";
builder.Services.AddCors(options =>
{
    options.AddPolicy(WasmDevelopmentCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:5074", "https://localhost:7024")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TillAppDbContext>();
        await dbContext.Database.MigrateAsync();
        await DevelopmentDataSeeder.SeedAsync(dbContext);
    }

    app.MapOpenApi();
    app.UseCors(WasmDevelopmentCorsPolicy);
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
