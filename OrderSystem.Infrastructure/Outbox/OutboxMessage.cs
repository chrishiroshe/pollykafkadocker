namespace OrderSystem.Infrastructure.Outbox
{
    public class OutboxMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        public string Type { get; set; } = default!;
        public string Payload { get; set; } = default!;

        public DateTime? ProcessedAtUtc { get; set; }

        public int Attempts { get; set; } = 0;
        public string? LastError { get; set; }

        public DateTime? LockedUntilUtc { get; set; }     // lease
        public string? LockedBy { get; set; }             // id do worker
        public DateTime? DeadLetteredAtUtc { get; set; }  // “DLQ”
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime? NextAttemptAtUtc { get; set; }

        public string? TraceId { get; set; }
    }
}
