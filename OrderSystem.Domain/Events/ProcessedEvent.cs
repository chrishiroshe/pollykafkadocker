namespace OrderSystem.Domain.Events
{
    public class ProcessedEvent
    {
        public Guid EventId { get; set; }
        public DateTime ProcessedAtUtc { get; set; }
        public string CorrelationId { get; set; } = default!;
    }
}
