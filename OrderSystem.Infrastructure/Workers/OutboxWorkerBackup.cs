//namespace OrderSystem.Infrastructure.Workers
//{ 
//    using Microsoft.EntityFrameworkCore;
//    using Microsoft.Extensions.DependencyInjection;
//    using Microsoft.Extensions.Hosting;
//    using Microsoft.Extensions.Logging;
//    using Microsoft.Extensions.Options;
//    using Order.Api.Infraestruture;
//    using OrderSystem.Domain.Enums;
//    using OrderSystem.Domain.Events;
//    using OrderSystem.Infrastructure.ExternalServices;
//    using OrderSystem.Infrastructure.Outbox;
//    using OrderSystem.Infrastructure.Persistence;
//    using System.Diagnostics;
//    using System.Text.Json;

//    public class OutboxWorker : BackgroundService
//    {
//        private readonly IServiceScopeFactory _scopeFactory;
//        private readonly ILogger<OutboxWorker> _logger;
//        private readonly OutboxMetrics _metrics;
//        private readonly string _workerId = Guid.NewGuid().ToString("N");
//        private static readonly TimeSpan LeaseTime = TimeSpan.FromSeconds(30);
//        private const int MaxAttempts = 5;
//        private readonly OutboxOptions _options;

//        public OutboxWorker(IServiceScopeFactory scopeFactory, ILogger<OutboxWorker> logger, OutboxMetrics metrics, IOptions<OutboxOptions> options)
//        {
//            _scopeFactory = scopeFactory;
//            _logger = logger;
//            _metrics = metrics;
//            _options = options.Value;
//        }

//        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        //{
//        //    _logger.LogInformation("Outbox Worker started ({WorkerId})", _workerId);

//        //    while (!stoppingToken.IsCancellationRequested)
//        //    {
//        //        using var scope = _scopeFactory.CreateScope();
//        //        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//        //        var batch = await AcquireBatchAsync(db, stoppingToken);

//        //        foreach (var msg in batch)
//        //        {
//        //            try
//        //            {
//        //                await HandleOneAsync(db, msg, stoppingToken);
//        //                _logger.LogInformation("Processed outbox {Id}", msg.Id);
//        //            }
//        //            catch (OperationCanceledException)
//        //            {
//        //                throw;
//        //            }
//        //            catch (Exception ex)
//        //            {
//        //                _logger.LogWarning(ex, "Failed processing outbox {Id} (attempt {Attempts})", msg.Id, msg.Attempts);
//        //            }
//        //        }

//        //        await Task.Delay(1000, stoppingToken);
//        //    }
//        //}

//        private async Task<List<OutboxMessage>> AcquireBatchAsync(AppDbContext db, CancellationToken ct)
//        {
//            var now = DateTime.UtcNow;

//            var batch = await db.OutboxMessages
//                .Where(m => m.ProcessedAtUtc == null
//                            && m.DeadLetteredAtUtc == null
//                            && (m.LockedUntilUtc == null || m.LockedUntilUtc < now)
//                            && m.Attempts < MaxAttempts)
//                .OrderBy(m => m.OccurredAtUtc)
//                .Take(10)
//                .ToListAsync(ct);

//            if (batch.Count == 0) return batch;

//            foreach (var m in batch)
//            {
//                m.LockedBy = _workerId;
//                m.LockedUntilUtc = now.Add(LeaseTime);
//            }

//            await db.SaveChangesAsync(ct);
//            return batch;
//        }
//        //private async Task HandleOneAsync(AppDbContext db, OutboxMessage message, CancellationToken ct)
//        //{
//        //    await using var tx = await db.Database.BeginTransactionAsync(ct);

//        //    try
//        //    {
//        //        await DispatchAsync(db, message, ct);

//        //        message.ProcessedAtUtc = DateTime.UtcNow;
//        //        message.LockedBy = null;
//        //        message.LockedUntilUtc = null;

//        //        await db.SaveChangesAsync(ct);
//        //        await tx.CommitAsync(ct);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        message.Attempts++;
//        //        message.LastError = ex.Message;

//        //        // Se estourou tentativas → DLQ
//        //        if (message.Attempts >= MaxAttempts)
//        //            message.DeadLetteredAtUtc = DateTime.UtcNow;

//        //        // Libera lock para outro retry futuro
//        //        message.LockedBy = null;
//        //        message.LockedUntilUtc = null;

//        //        await db.SaveChangesAsync(ct);
//        //        await tx.RollbackAsync(ct);

//        //        throw;
//        //    }
//        //}
//        //private async Task DispatchAsync(AppDbContext db, OutboxMessage message, CancellationToken ct)
//        //{
//        //    if (message.Type == "OrderConfirmed")
//        //    {
//        //        var evt = JsonSerializer.Deserialize<OrderConfirmedEvent>(message.Payload)!;
//        //        var order = await db.Orders.FirstAsync(o => o.Id == evt.OrderId, ct);

//        //        // ✅ Idempotência no domínio: se já está Paid/Failed, não faz nada
//        //        if (order.Status is OrderStatus.Paid or OrderStatus.Failed)
//        //            return;

//        //        // simulação de pagamento por enquanto
//        //        var success = Random.Shared.Next(0, 2) == 0;
//        //        if (success) order.MarkAsPaid(DateTime.UtcNow);
//        //        else order.Fail("Payment refused");

//        //        return;
//        //    }

//        //    throw new InvalidOperationException($"Unknown outbox message type: {message.Type}");
//        //}
//        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        //{
//        //    _logger.LogInformation("Outbox Worker started");

//        //    while (!stoppingToken.IsCancellationRequested)
//        //    {
//        //        try
//        //        {
//        //            using var scope = _scopeFactory.CreateScope();
//        //            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//        //            var paymentGateway = scope.ServiceProvider.GetRequiredService<PaymentGateway>();


//        //            // 1) Transação curta: pegar + processar + marcar processado
//        //            await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

//        //            // 2) Pega 1 mensagem disponível e TRAVA a linha (FOR UPDATE),
//        //            // mas se outra já estiver travada, SKIP LOCKED pula.
//        //            var msg = await db.OutboxMessages
//        //             .FromSqlInterpolated($@"
//        //            SELECT *
//        //            FROM ""OutboxMessages""
//        //            WHERE ""ProcessedAtUtc"" IS NULL
//        //              AND ""DeadLetteredAtUtc"" IS NULL
//        //              AND (""NextAttemptAtUtc"" IS NULL OR ""NextAttemptAtUtc"" <= NOW())
//        //            ORDER BY ""OccurredAtUtc""
//        //            FOR UPDATE SKIP LOCKED
//        //            LIMIT 1
//        //        ")
//        //             .FirstOrDefaultAsync(stoppingToken);

//        //            if (msg is null)
//        //            {
//        //                await tx.CommitAsync(stoppingToken);
//        //                await Task.Delay(800, stoppingToken);
//        //                continue;
//        //            }

//        //            using var activity = new Activity("OutboxProcessing");
//        //            activity.SetIdFormat(ActivityIdFormat.W3C);

//        //            if (!string.IsNullOrWhiteSpace(msg.TraceId))
//        //            {
//        //                activity.SetParentId(msg.TraceId);
//        //            }

//        //            activity.Start();

//        //            _logger.LogInformation(
//        //              "Processing outbox message {MessageId} (Attempt {Attempt})",
//        //              msg.Id,
//        //              msg.Attempts + 1);
//        //            try
//        //            {
//        //                // 3) Processa a mensagem (a linha está lockada para este tx)
//        //                await DispatchAsync(db, msg, paymentGateway, stoppingToken);

//        //                msg.ProcessedAtUtc = DateTime.UtcNow;

//        //                await db.SaveChangesAsync(stoppingToken);
//        //                await tx.CommitAsync(stoppingToken);

//        //                _metrics.IncrementProcessed();
//        //                _logger.LogInformation(
//        //                    "Outbox message {MessageId} processed successfully",
//        //                    msg.Id);
//        //            }
//        //            catch (OperationCanceledException)
//        //            {
//        //                _metrics.IncrementFailed();
//        //                _logger.LogWarning(
//        //                    "Processing cancelled for message {MessageId}",
//        //                    msg.Id);

//        //                throw;
//        //            }
//        //            catch (Exception ex)
//        //            {
//        //                _metrics.IncrementFailed();
//        //                msg.Attempts++;
//        //                msg.LastError = ex.Message;

//        //                if (msg.Attempts >= 5)
//        //                {
//        //                    msg.DeadLetteredAtUtc = DateTime.UtcNow;
//        //                    _metrics.IncrementDeadLettered();
//        //                    _logger.LogError(ex,
//        //                        "Outbox message {MessageId} moved to DLQ after {Attempts} attempts",
//        //                        msg.Id,
//        //                        msg.Attempts);
//        //                }
//        //                else
//        //                {
//        //                    msg.NextAttemptAtUtc = CalculateNextAttempt(msg.Attempts);

//        //                    _logger.LogWarning(ex,
//        //                        "Outbox message {MessageId} failed. Retry {Attempt} scheduled at {NextAttempt}",
//        //                        msg.Id,
//        //                        msg.Attempts,
//        //                        msg.NextAttemptAtUtc);
//        //                }


//        //                await db.SaveChangesAsync(stoppingToken);
//        //                await tx.CommitAsync(stoppingToken);

//        //                _logger.LogWarning("Retry scheduled for outbox {Id} at {NextAttempt}",
//        //                    msg.Id, msg.NextAttemptAtUtc);
//        //            }

//        //        }

//        //        catch (OperationCanceledException)
//        //        {
//        //            break;
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            _logger.LogError(ex, "Outbox Worker loop error. Retrying in 5 seconds...");
//        //            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//        //        }
//        //    }
//        //    _logger.LogInformation("Outbox Worker stopped");
//        //}

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("Outbox Worker started");

//            var parallelism = _options.MaxParallelism;

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    using var scope = _scopeFactory.CreateScope();
//                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//                    await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

//                    var ids = await AcquireBatchIdsAsync(db, stoppingToken);

//                    await tx.CommitAsync(stoppingToken);

//                    if (ids.Count == 0)
//                    {
//                        await Task.Delay(1000, stoppingToken);
//                        continue;
//                    }

//                    using var semaphore = new SemaphoreSlim(parallelism);

//                    var tasks = ids.Select(async id =>
//                    {
//                        await semaphore.WaitAsync(stoppingToken);
//                        try
//                        {
//                            await ProcessSingleMessageAsync(id, stoppingToken);
//                        }
//                        finally
//                        {
//                            semaphore.Release();
//                        }
//                    });

//                    await Task.WhenAll(tasks);
//                }
//                catch (OperationCanceledException)
//                {
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Outbox Worker loop error. Retrying in 5 seconds...");
//                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//                }
//            }

//            _logger.LogInformation("Outbox Worker stopped");
//        }
//        private static DateTime CalculateNextAttempt(int attempts)
//        {
//            const int baseDelaySeconds = 2;
//            const int maxDelaySeconds = 300; // 5 minutos máximo

//            // delay exponencial: base * 2^attempt
//            var exponentialDelay = baseDelaySeconds * Math.Pow(2, attempts);

//            // limita para não crescer infinito
//            var cappedDelay = Math.Min(exponentialDelay, maxDelaySeconds);

//            // FULL JITTER: random entre 0 e cappedDelay
//            var jitteredDelay = Random.Shared.NextDouble() * cappedDelay;

//            return DateTime.UtcNow.AddSeconds(jitteredDelay);
//        }
//        private async Task DispatchAsync(AppDbContext db, OutboxMessage msg, PaymentGateway paymentGateway, CancellationToken ct)
//        {
//            // Se já processado, sai
//            var already = await db.ProcessedEvents
//                .AnyAsync(x => x.EventId == msg.Id, ct);

//            if (already)
//                return;

//            if (msg.Type == "OrderConfirmed")
//            {
//                var evt = JsonSerializer.Deserialize<OrderConfirmedEvent>(msg.Payload)!;
//                var order = await db.Orders.FirstAsync(o => o.Id == evt.OrderId, ct);

//                if (order.Status is OrderStatus.Paid or OrderStatus.Failed)
//                    return;

//                try
//                {
//                    await paymentGateway.ChargeAsync(order.Id, ct);
//                    order.MarkAsPaid(DateTime.UtcNow);
//                }
//                catch
//                {
//                    order.Fail("Gateway failure");
//                    throw; // deixa retry/backoff agir
//                }
//            }

//            db.ProcessedEvents.Add(new ProcessedEvent
//            {
//                EventId = msg.Id,
//                ProcessedAtUtc = DateTime.UtcNow
//            });
//        }
//        private async Task<List<Guid>> AcquireBatchIdsAsync(AppDbContext db, CancellationToken ct)
//        {
//            return await db.OutboxMessages
//                .FromSqlInterpolated($@"
//            SELECT *
//            FROM ""OutboxMessages""
//            WHERE ""ProcessedAtUtc"" IS NULL
//              AND ""DeadLetteredAtUtc"" IS NULL
//              AND ""Attempts"" < {MaxAttempts}
//              AND (""NextAttemptAtUtc"" IS NULL OR ""NextAttemptAtUtc"" <= NOW())
//            ORDER BY ""OccurredAtUtc""
//            FOR UPDATE SKIP LOCKED
//           LIMIT {_options.BatchSize}
//        ")
//                .Select(x => x.Id)
//                .ToListAsync(ct);
//        }

//        private async Task ProcessSingleMessageAsync(Guid messageId, CancellationToken ct)
//        {
//            using var scope = _scopeFactory.CreateScope();

//            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//            var paymentGateway = scope.ServiceProvider.GetRequiredService<PaymentGateway>();

//            await using var tx = await db.Database.BeginTransactionAsync(ct);

//            var msg = await db.OutboxMessages.FirstOrDefaultAsync(x => x.Id == messageId, ct);

//            if (msg is null)
//                return;

//            using var activity = new Activity("OutboxProcessing");
//            activity.SetIdFormat(ActivityIdFormat.W3C);

//            if (!string.IsNullOrWhiteSpace(msg.TraceId))
//                activity.SetParentId(msg.TraceId);

//            activity.Start();

//            try
//            {
//                await DispatchAsync(db, msg, paymentGateway, ct);

//                msg.ProcessedAtUtc = DateTime.UtcNow;

//                await db.SaveChangesAsync(ct);
//                await tx.CommitAsync(ct);

//                _metrics.IncrementProcessed();

//                _logger.LogInformation(
//                    "Outbox message {MessageId} processed successfully",
//                    msg.Id);
//            }
//            catch (Exception ex)
//            {
//                _metrics.IncrementFailed();

//                msg.Attempts++;
//                msg.LastError = ex.Message;

//                if (msg.Attempts >= MaxAttempts)
//                {
//                    msg.DeadLetteredAtUtc = DateTime.UtcNow;
//                    _metrics.IncrementDeadLettered();

//                    _logger.LogError(ex,
//                        "Outbox message {MessageId} moved to DLQ after {Attempts} attempts",
//                        msg.Id,
//                        msg.Attempts);
//                }
//                else
//                {
//                    msg.NextAttemptAtUtc = CalculateNextAttempt(msg.Attempts);

//                    _logger.LogWarning(ex,
//                        "Outbox message {MessageId} failed. Retry {Attempt} scheduled at {NextAttempt}",
//                        msg.Id,
//                        msg.Attempts,
//                        msg.NextAttemptAtUtc);
//                }

//                await db.SaveChangesAsync(ct);
//                await tx.CommitAsync(ct);
//            }
//        }
//    }
//}
