using Microsoft.EntityFrameworkCore;
using Order.Api.Infraestruture;
using Polly;
using Polly.Extensions.Http;
using System.Net;
using Microsoft.Extensions.Logging;
using OrderSystem.Application.Abstractions;
using OrderSystem.Application.Services;
using OrderSystem.Infrastructure.Outbox;
using OrderSystem.Infrastructure.Repositories;
using OrderSystem.Infrastructure.Workers;
using OrderSystem.Infrastructure.Persistence;
using OrderSystem.Infrastructure.ExternalServices;
using Microsoft.Extensions.Options;
using OrderSystem.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);
var runMode = builder.Configuration["RunMode"] ?? "api";
Console.WriteLine("Run mODE : { runMode}");

if (runMode == "api" )
{
    builder.Services.AddControllers();


    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Order API", Version = "v1" });
    });

}
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
       builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.MigrationsAssembly("OrderSystem.Infrastructure")));


builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxWriter, OutboxWriter>();
builder.Services.AddScoped<OrdersService>();

//builder.Services.AddHostedService<OutboxWorker>();
var disableWorkers = builder.Configuration.GetValue<bool>("DisableWorkers");

builder.Services.Configure<KafkaOptions>(
    builder.Configuration.GetSection("Kafka"));

builder.Services.AddSingleton<KafkaPublisher>();


if (runMode == "worker" )
{
    builder.Services.AddHostedService<OutboxPublisherWorker>();
    builder.Services.AddHostedService<PaymentConsumerWorker>();
    builder.Services.AddHostedService<OrderStatusUpdaterWorker>();
    builder.Services.AddHostedService<SagaOrchestratorWorker>();
    builder.Services.AddHostedService<InventoryWorker>();
    builder.Services.AddHostedService<PaymentConsumerWorker>();
    builder.Services.AddHostedService<OrderStatusUpdaterWorker>();
}
builder.Services.AddHostedService<OutboxCleanupWorker>();
builder.Services.AddSingleton<OutboxMetrics>();
builder.Services.Configure<OutboxOptions>(
    builder.Configuration.GetSection("Outbox"));

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});
builder.Services.Configure<PaymentGatewayOptions>(
    builder.Configuration.GetSection("PaymentGateway"));

builder.Services.AddHttpClient<PaymentGateway>((sp, client) =>
{
    var options = sp
       .GetRequiredService<IOptions<PaymentGatewayOptions>>()
       .Value;
    // melhor vir do appsettings, mas pode hardcode por enquanto
    //var baseUrl = builder.Configuration["PaymentGateway:BaseUrl"] ?? "https://localhost:5001/";
    //client.BaseAddress = new Uri(baseUrl);
    //client.Timeout = TimeSpan.FromSeconds(5);
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler((sp, request) => GetCircuitBreakerPolicy(sp));

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "database",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
    .AddCheck<OutboxHealthCheck>("outbox");

var app = builder.Build();
//garante migration subindo
if (runMode == "migration")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        Console.WriteLine("Migration complete successfully.");
        Thread.Sleep(2000);

        Environment.Exit(0);
        
    }
}
if (runMode == "api" )
{
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.MapHealthChecks("/health/ready");

    app.MapGet("/metrics", (OutboxMetrics metrics) =>
    {
        return Results.Text($@"
        outbox_processed_total {metrics.Processed}
        outbox_failed_total {metrics.Failed}
        outbox_deadletter_total {metrics.DeadLettered}
        ");
    });
    // Normalmente só em dev
    //if (app.Environment.IsDevelopment())
    //{
    app.UseSwagger();
    app.UseSwaggerUI();
    //}
    // Configure the HTTP request pipeline.
    //if (app.Environment.IsDevelopment())
    //{
    //    app.MapOpenApi();
    ////}

    //if (!app.Environment.IsEnvironment("Docker"))
    //{
    //    app.UseHttpsRedirection();
    //}

    app.Use(async (context, next) =>
    {
        Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");
        await next();
        Console.WriteLine($"Response: {context.Response.StatusCode}");
    });

    app.MapControllers();

}
app.Run();


static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    var random = new Random();

    return HttpPolicyExtensions
        .HandleTransientHttpError() // 5xx + 408 + network errors
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
            {
                var exponentialBackoff = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                var jitter = TimeSpan.FromMilliseconds(random.Next(0, 1000));
                return exponentialBackoff + jitter;
            });
}


// ...

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider sp)
{
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("PaymentCircuit");

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(15),
            onBreak: (outcome, breakDelay) =>
            {
                var reason = outcome.Exception?.Message
                             ?? $"HTTP {(int)outcome.Result.StatusCode}";
                logger.LogWarning("Circuit OPEN for {BreakDelay}. Reason: {Reason}", breakDelay, reason);
            },
            onReset: () =>
            {
                logger.LogInformation("Circuit CLOSED (reset).");
            },
            onHalfOpen: () =>
            {
                logger.LogInformation("Circuit HALF-OPEN (testing).");
            }
        );

}
public partial class Program { }