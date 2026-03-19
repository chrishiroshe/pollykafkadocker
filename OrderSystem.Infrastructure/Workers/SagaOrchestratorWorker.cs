using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderSystem.Domain.Events;
using OrderSystem.Infrastructure.Messaging;
using OrderSystem.Infrastructure.Persistence;
using OrderSystem.Infrastructure.Sagas;
using System.Text.Json;

namespace OrderSystem.Infrastructure.Workers;

public class SagaOrchestratorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SagaOrchestratorWorker> _logger;
    private readonly KafkaOptions _options;

    public SagaOrchestratorWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SagaOrchestratorWorker> logger,
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
            GroupId = "saga-orchestrator",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(new[]
        {
            _options.OrdersConfirmedTopic,
            _options.InventoryReservedTopic,
            _options.PaymentFailedTopic,
            _options.InventoryReservationFailedTopic,
            _options.InventoryReleasedTopic
        });

        _logger.LogInformation("Saga Orchestrator Worker started");

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
                var publisher = scope.ServiceProvider.GetRequiredService<KafkaPublisher>();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                switch (result.Topic)
                {
                    case "orders-confirmed":
                        {
                            var evt = JsonSerializer.Deserialize<OrderConfirmedEvent>(result.Message.Value)
                                      ?? throw new InvalidOperationException("Invalid payload for orders-confirmed");

                            var saga = await db.SagaStates.FirstOrDefaultAsync(x => x.OrderId == evt.OrderId, stoppingToken);

                            if (saga is null)
                            {
                                saga = new SagaState
                                {
                                    OrderId = evt.OrderId,
                                    CurrentStep = "InventoryReservationRequested",
                                    Status = "Running",
                                    CreatedAtUtc = DateTime.UtcNow,
                                    UpdatedAtUtc = DateTime.UtcNow
                                };

                                db.SagaStates.Add(saga);
                            }
                            else
                            {
                                saga.CurrentStep = "InventoryReservationRequested";
                                saga.Status = "Running";
                                saga.UpdatedAtUtc = DateTime.UtcNow;
                                saga.LastError = null;
                            }

                            var next = new InventoryReserveRequestedEvent
                            {
                                OrderId = evt.OrderId
                            };

                            await publisher.PublishAsync(
                                "inventory-reserve-requested",
                                evt.OrderId.ToString(),
                                JsonSerializer.Serialize(next));

                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Saga: published inventory-reserve-requested for Order {OrderId}",
                                evt.OrderId);

                            break;
                        }
                    case "inventory-reserved":
                        {
                            var evt = JsonSerializer.Deserialize<InventoryReservedEvent>(result.Message.Value)
                                      ?? throw new InvalidOperationException("Invalid payload for inventory-reserved");

                            var saga = await db.SagaStates.FirstOrDefaultAsync(x => x.OrderId == evt.OrderId, stoppingToken);

                            if (saga is not null)
                            {
                                saga.CurrentStep = "PaymentRequested";
                                saga.Status = "Running";
                                saga.UpdatedAtUtc = DateTime.UtcNow;
                                saga.LastError = null;
                            }

                            var next = new PaymentRequestedEvent
                            {
                                OrderId = evt.OrderId
                            };

                            await publisher.PublishAsync(
                                "payment-requested",
                                evt.OrderId.ToString(),
                                JsonSerializer.Serialize(next));

                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Saga: published payment-requested for Order {OrderId}",
                                evt.OrderId);

                            break;
                        }

                    case "payment-failed":
                        {
                            var evt = JsonSerializer.Deserialize<PaymentFailedEvent>(result.Message.Value)
                                      ?? throw new InvalidOperationException("Invalid payload for payment-failed");

                            var saga = await db.SagaStates.FirstOrDefaultAsync(x => x.OrderId == evt.OrderId, stoppingToken);

                            if (saga is not null)
                            {
                                saga.CurrentStep = "InventoryReleaseRequested";
                                saga.Status = "Compensating";
                                saga.UpdatedAtUtc = DateTime.UtcNow;
                                saga.LastError = evt.Reason;
                            }

                            var compensation = new InventoryReleaseRequestedEvent
                            {
                                OrderId = evt.OrderId
                            };

                            await publisher.PublishAsync(
                                "inventory-release-requested",
                                evt.OrderId.ToString(),
                                JsonSerializer.Serialize(compensation));

                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Saga: published inventory-release-requested for Order {OrderId}",
                                evt.OrderId);

                            break;
                        }
                    case "inventory-reservation-failed":
                        {
                            var evt = JsonSerializer.Deserialize<InventoryReservationFailedEvent>(result.Message.Value)
                                      ?? throw new InvalidOperationException("Invalid payload for inventory-reservation-failed");

                            var saga = await db.SagaStates.FirstOrDefaultAsync(x => x.OrderId == evt.OrderId, stoppingToken);

                            if (saga is not null)
                            {
                                saga.CurrentStep = "InventoryReservationFailed";
                                saga.Status = "Failed";
                                saga.UpdatedAtUtc = DateTime.UtcNow;
                                saga.LastError = evt.Reason;
                            }

                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogWarning(
                                "Saga failed during inventory reservation for Order {OrderId}. Reason: {Reason}",
                                evt.OrderId,
                                evt.Reason);

                            break;
                        }
                    case "inventory-released":
                        {
                            var evt = JsonSerializer.Deserialize<InventoryReleasedEvent>(result.Message.Value)
                                      ?? throw new InvalidOperationException("Invalid payload");

                            var saga = await db.SagaStates.FirstOrDefaultAsync(x => x.OrderId == evt.OrderId, stoppingToken);

                            if (saga is not null)
                            {
                                saga.CurrentStep = "CompensationCompleted";
                                saga.Status = "Compensated";
                                saga.UpdatedAtUtc = DateTime.UtcNow;
                            }

                            await db.SaveChangesAsync(stoppingToken);

                            _logger.LogInformation(
                                "Saga compensation completed for Order {OrderId}",
                                evt.OrderId);

                            break;
                        }

                    default:
                        {
                            _logger.LogWarning("Saga received message from unexpected topic {Topic}", result.Topic);
                            break;
                        }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga Orchestrator Worker error");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Saga Orchestrator Worker stopped");
    }
}