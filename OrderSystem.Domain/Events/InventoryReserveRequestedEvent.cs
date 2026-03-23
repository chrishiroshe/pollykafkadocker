namespace OrderSystem.Domain.Events;

public class InventoryReserveRequestedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = default!;
}