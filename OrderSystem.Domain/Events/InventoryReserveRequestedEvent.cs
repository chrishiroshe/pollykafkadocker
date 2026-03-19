namespace OrderSystem.Domain.Events;

public class InventoryReserveRequestedEvent
{
    public Guid OrderId { get; set; }
}