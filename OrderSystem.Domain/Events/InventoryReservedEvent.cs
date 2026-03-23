namespace OrderSystem.Domain.Events;

public class InventoryReservedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = default!;
}