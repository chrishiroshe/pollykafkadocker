using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Order.Api.Infraestruture;
using OrderSystem.Infrastructure.Messaging;
using OrderSystem.Infrastructure.Observability;
using OrderSystem.Infrastructure.Outbox;
using OrderSystem.Infrastructure.Persistence;

namespace OrderSystem.Infrastructure.Workers;

public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherWorker> _logger;
    //private readonly OutboxMetrics _metrics;
  
    private readonly OutboxOptions _outboxOptions;
    private readonly KafkaOptions _kafkaOptions;

    private const int MaxAttempts = 5;

    public OutboxPublisherWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxPublisherWorker> logger,
       // OutboxMetrics metrics,
        IOptions<OutboxOptions> outboxOptions,
        IOptions<KafkaOptions> kafkaOptions)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        //_metrics = metrics;
        _outboxOptions = outboxOptions.Value;
        _kafkaOptions = kafkaOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Publisher Worker started");

        var parallelism = _outboxOptions.MaxParallelism;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

                var ids = await db.OutboxMessages
                    .FromSqlInterpolated($@"
                        SELECT *
                        FROM ""OutboxMessages""
                        WHERE ""ProcessedAtUtc"" IS NULL
                          AND ""DeadLetteredAtUtc"" IS NULL
                          AND ""Attempts"" < {MaxAttempts}
                          AND (""NextAttemptAtUtc"" IS NULL OR ""NextAttemptAtUtc"" <= NOW())
                        ORDER BY ""OccurredAtUtc""
                        FOR UPDATE SKIP LOCKED
                        LIMIT {_outboxOptions.BatchSize}
                    ")
                    .Select(x => x.Id)
                    .ToListAsync(stoppingToken);

                await tx.CommitAsync(stoppingToken);

                if (ids.Count == 0)
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                using var semaphore = new SemaphoreSlim(parallelism);

                var tasks = ids.Select(async id =>
                {
                    await semaphore.WaitAsync(stoppingToken);
                    try
                    {
                        await PublishSingleMessageAsync(id, stoppingToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox Publisher loop error. Retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Outbox Publisher Worker stopped");
    }

    private async Task PublishSingleMessageAsync(Guid messageId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<KafkaPublisher>();

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var msg = await db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, ct);
        if (msg is null)
            return;

        try
        {
            await publisher.PublishAsync(
                _kafkaOptions.OrdersConfirmedTopic,
                msg.Id.ToString(),
                msg.Payload);

            msg.ProcessedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            MetricsRegistry.OutboxProcessed.Inc();
           // _metrics.IncrementProcessed();

            _logger.LogInformation("Outbox message {MessageId} published to Kafka", msg.Id);
        }
        catch (Exception ex)
        {
            MetricsRegistry.OutboxFailed.Inc();
           // _metrics.IncrementFailed();

            msg.Attempts++;
            msg.LastError = ex.Message;

            if (msg.Attempts >= MaxAttempts)
            {
                msg.DeadLetteredAtUtc = DateTime.UtcNow;
                //  _metrics.IncrementDeadLettered();
                MetricsRegistry.OutboxDeadLetter.Inc();

            }
            else
            {
                msg.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(5);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogError(ex, "Failed to publish outbox message {MessageId}", msg.Id);
        }
    }
}