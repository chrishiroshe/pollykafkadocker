namespace OrderSystem.Domain.Events
{
    public sealed record OrderConfirmedEvent(Guid OrderId, DateTime ConfirmedAtUtc, string CorrelationId );
 
}
