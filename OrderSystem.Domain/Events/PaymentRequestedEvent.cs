namespace OrderSystem.Domain.Events;

public class PaymentRequestedEvent
{
    public Guid OrderId { get; set; }
    public string CorrelationId { get; set; } = default!;
}