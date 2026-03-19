namespace OrderSystem.Domain.Events;

public class PaymentRequestedEvent
{
    public Guid OrderId { get; set; }
}