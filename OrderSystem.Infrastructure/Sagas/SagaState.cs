namespace OrderSystem.Infrastructure.Sagas;

public class SagaState
{
    public Guid OrderId { get; set; }

    public string CurrentStep { get; set; } = default!;
    public string Status { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public string? LastError { get; set; }
}