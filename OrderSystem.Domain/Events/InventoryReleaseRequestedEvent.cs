namespace OrderSystem.Domain.Events;

public class InventoryReleaseRequestedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = default!;
}