using OrderSystem.Domain.Enums;

namespace OrderSystem.Domain.Entities
{
    public class Order
    {
        public Guid Id { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public OrderStatus Status { get; private set; }
        public DateTime? PaidAt { get; private set; }
        public string? FailureReason { get; private set; }

        public Order()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Status = OrderStatus.Draft;
        }

        public void Confirm()
        {
            if (Status != OrderStatus.Draft)
                throw new InvalidOperationException("Only draft orders can be confirmed.");
          
            Status = OrderStatus.Confirmed;
            FailureReason = null;
        }
        public void MarkAsPaid(DateTime paidAtUtc)
        {
            if (Status != OrderStatus.Confirmed)
                throw new InvalidOperationException("Only confirmed orders can be marked as paid.");

            Status = OrderStatus.Paid;
            PaidAt = paidAtUtc;
            FailureReason = null;
        }

        public void Fail(string reason)
        {
            if (Status != OrderStatus.Confirmed)
                throw new InvalidOperationException("Only confirmed orders can fail.");

            if (string.IsNullOrWhiteSpace(reason))
                reason = "Unknown";

            Status = OrderStatus.Failed;
            FailureReason = reason;
        }
    }
}
