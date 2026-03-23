namespace OrderSystem.Domain.Events;

public class InventoryReservationFailedEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = "";
    public string CorrelationId { get; set; } = default!;
}