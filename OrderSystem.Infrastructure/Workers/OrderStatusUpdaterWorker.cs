using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderSystem.Domain.Enums;
using OrderSystem.Domain.Events;
using OrderSystem.Infrastructure.Messaging;
using OrderSystem.Infrastructure.Persistence;
using System.Text.Json;

namespace OrderSystem.Infrastructure.Workers;

public class OrderStatusUpdaterWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderStatusUpdaterWorker> _logger;
    private readonly KafkaOptions _options;

    public OrderStatusUpdaterWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderStatusUpdaterWorker> logger,
        IOptions<KafkaOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = "orders",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(new[]
        {
            _options.PaymentSucceededTopic,
            _options.PaymentFailedTopic
        });

        _logger.LogInformation("Order Status Updater Worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));

                if (result is null)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (result.Topic == "payment-succeeded")
                {
                    var evt = JsonSerializer.Deserialize<PaymentSucceededEvent>(result.Message.Value)
                              ?? throw new InvalidOperationException("Invalid payload for payment-succeeded");

                    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, stoppingToken);

                    if (order is null)
                    {
                        _logger.LogWarning(
                            "Order {OrderId} not found while handling payment-succeeded",
                            evt.OrderId);
                        continue;
                    }

                    if (order.Status == OrderStatus.Paid)
                    {
                        _logger.LogInformation(
                            "Order {OrderId} is already Paid. Ignoring duplicate event.",
                            evt.OrderId);
                        continue;
                    }

                    if (order.Status == OrderStatus.Failed)
                    {
                        _logger.LogWarning(
                            "Order {OrderId} is already Failed. Ignoring payment-succeeded event.",
                            evt.OrderId);
                        continue;
                    }

                    order.MarkAsPaid(evt.PaidAtUtc);

                    var saga = await db.SagaStates.FirstOrDefaultAsync(x => x.OrderId == evt.OrderId, stoppingToken);

                    if (saga is not null)
                    {
                        saga.CurrentStep = "Completed";
                        saga.Status = "Completed";
                        saga.UpdatedAtUtc = DateTime.UtcNow;
                        saga.LastError = null;
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Order {OrderId} marked as Paid",
                        evt.OrderId);
                }

                if (result.Topic == "payment-failed")
                {
                    var evt = JsonSerializer.Deserialize<PaymentFailedEvent>(result.Message.Value)
                              ?? throw new InvalidOperationException("Invalid payload for payment-failed");

                    var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == evt.OrderId, stoppingToken);

                    if (order is null)
                    {
                        _logger.LogWarning(
                            "Order {OrderId} not found while handling payment-failed",
                            evt.OrderId);
                        continue;
                    }

                    if (order.Status == OrderStatus.Failed)
                    {
                        _logger.LogInformation(
                            "Order {OrderId} is already Failed. Ignoring duplicate event.",
                            evt.OrderId);
                        continue;
                    }

                    if (order.Status == OrderStatus.Paid)
                    {
                        _logger.LogWarning(
                            "Order {OrderId} is already Paid. Ignoring payment-failed event.",
                            evt.OrderId);
                        continue;
                    }

                    order.Fail(evt.Reason);

                    var saga = await db.SagaStates.FirstOrDefaultAsync(x => x.OrderId == evt.OrderId, stoppingToken);

                    if (saga is not null)
                    {
                        saga.CurrentStep = "Compensating";
                        saga.Status = "Failed";
                        saga.UpdatedAtUtc = DateTime.UtcNow;
                        saga.LastError = null;
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Order {OrderId} marked as Failed",
                        evt.OrderId);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order Status Updater Worker error");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Order Status Updater Worker stopped");
    }
}