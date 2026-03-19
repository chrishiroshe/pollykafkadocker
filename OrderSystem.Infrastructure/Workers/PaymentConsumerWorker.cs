using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderSystem.Domain.Events;
using OrderSystem.Infrastructure.ExternalServices;
using OrderSystem.Infrastructure.Messaging;
using System.Text.Json;

namespace OrderSystem.Infrastructure.Workers;

public class PaymentConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentConsumerWorker> _logger;
    private readonly KafkaOptions _options;

    public PaymentConsumerWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentConsumerWorker> logger,
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
            GroupId = "payments",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        // Na saga, o payment consumer consome "payment-requested"
        consumer.Subscribe(_options.PaymentRequestedTopic);

        _logger.LogInformation("Payment Consumer Worker started");

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

                var evt = JsonSerializer.Deserialize<PaymentRequestedEvent>(result.Message.Value)
                          ?? throw new InvalidOperationException("Invalid Kafka payload for payment-requested");

                using var scope = _scopeFactory.CreateScope();
                var paymentGateway = scope.ServiceProvider.GetRequiredService<PaymentGateway>();
                var publisher = scope.ServiceProvider.GetRequiredService<KafkaPublisher>();

                try
                {
                    await paymentGateway.ChargeAsync(evt.OrderId, stoppingToken);

                    var successEvent = new PaymentSucceededEvent
                    {
                        OrderId = evt.OrderId,
                        PaidAtUtc = DateTime.UtcNow
                    };

                    await publisher.PublishAsync(
                        "payment-succeeded",
                        evt.OrderId.ToString(),
                        JsonSerializer.Serialize(successEvent));

                    _logger.LogInformation(
                        "Published payment-succeeded for Order {OrderId}",
                        evt.OrderId);
                }
                catch (Exception ex)
                {
                    var failEvent = new PaymentFailedEvent
                    {
                        OrderId = evt.OrderId,
                        Reason = "Gateway failure"
                    };

                    await publisher.PublishAsync(
                        "payment-failed",
                        evt.OrderId.ToString(),
                        JsonSerializer.Serialize(failEvent));

                    _logger.LogWarning(ex,
                        "Published payment-failed for Order {OrderId}",
                        evt.OrderId);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment Consumer error");
                await Task.Delay(1000, stoppingToken);
            }
        }

        _logger.LogInformation("Payment Consumer Worker stopped");
    }
}