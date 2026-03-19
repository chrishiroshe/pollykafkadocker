namespace OrderSystem.Domain.Events;

public class InventoryReleaseRequestedEvent
{
    public Guid OrderId { get; set; }
}