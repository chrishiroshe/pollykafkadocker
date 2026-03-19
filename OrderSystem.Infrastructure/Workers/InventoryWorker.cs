using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderSystem.Domain.Events;
using OrderSystem.Infrastructure.Messaging;
using System.Text.Json;

namespace OrderSystem.Infrastructure.Workers;

public class InventoryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InventoryWorker> _logger;
    private readonly KafkaOptions _options;

    public InventoryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<InventoryWorker> logger,
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
            GroupId = "inventory-service",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(new[]
        {
            _options.InventoryReserveRequestedTopic,
            _options.InventoryReleaseRequestedTopic
        });

        _logger.LogInformation("Inventory Worker started");

        _logger.LogInformation("Inventory Worker started");

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

                switch (result.Topic)
                {
                    case "inventory-reserve-requested":
                        {
                            var evt = JsonSerializer.Deserialize<InventoryReserveRequestedEvent>(result.Message.Value)
                                      ?? throw new InvalidOperationException("Invalid payload for inventory-reserve-requested");

                            // Simulação: 80% sucesso, 20% falha
                            var success = Random.Shared.Next(1, 11) <= 8;

                            if (success)
                            {
                                var reserved = new InventoryReservedEvent
                                {
                                    OrderId = evt.OrderId
                                };

                                await publisher.PublishAsync(
                                    "inventory-reserved",
                                    evt.OrderId.ToString(),
                                    JsonSerializer.Serialize(reserved));

                                _logger.LogInformation(
                                    "Inventory reserved for Order {OrderId}",
                                    evt.OrderId);
                            }
                            else
                            {
                                var failed = new InventoryReservationFailedEvent
                                {
                                    OrderId = evt.OrderId,
                                    Reason = "Insufficient stock"
                                };

                                await publisher.PublishAsync(
                                    "inventory-reservation-failed",
                                    evt.OrderId.ToString(),
                                    JsonSerializer.Serialize(failed));

                                _logger.LogWarning(
                                    "Inventory reservation failed for Order {OrderId}",
                                    evt.OrderId);
                            }

                            break;
                        }
                    case "inventory-release-requested":
                        {
                            var evt = JsonSerializer.Deserialize<InventoryReleaseRequestedEvent>(result.Message.Value)
                                      ?? throw new InvalidOperationException("Invalid payload");

                            var released = new InventoryReleasedEvent
                            {
                                OrderId = evt.OrderId,
                                ReleasedAtUtc = DateTime.UtcNow
                            };

                            await publisher.PublishAsync(
                                "inventory-released",
                                evt.OrderId.ToString(),
                                JsonSerializer.Serialize(released));

                            _logger.LogInformation(
                                "Inventory released for Order {OrderId}",
                                evt.OrderId);

                            break;
                        }
                    default:
                        {
                            _logger.LogWarning(
                                "Inventory Worker received message from unexpected topic {Topic}",
                                result.Topic);

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
                _logger.LogError(ex, "Inventory Worker error");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Inventory Worker stopped");
    }
}