namespace Order.Api.Infraestruture
{
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.EntityFrameworkCore;
    using OrderSystem.Infrastructure.Persistence;

    public class OutboxHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _db;

        public OutboxHealthCheck(AppDbContext db)
        {
            _db = db;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var pendingCount = await _db.OutboxMessages
                .Where(x => x.ProcessedAtUtc == null && x.DeadLetteredAtUtc == null)
                .CountAsync(cancellationToken);

            if (pendingCount > 100)
            {
                return HealthCheckResult.Degraded(
                    $"Too many pending outbox messages: {pendingCount}");
            }

            return HealthCheckResult.Healthy("Outbox OK");
        }
    }
}
