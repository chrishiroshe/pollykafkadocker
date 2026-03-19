namespace OrderSystem.Domain.Events;

public class InventoryReleasedEvent
{
    public Guid OrderId { get; set; }
    public DateTime ReleasedAtUtc { get; set; }
}