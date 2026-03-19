namespace OrderSystem.Infrastructure.Outbox;

public class OutboxOptions
{
    public int MaxParallelism { get; set; } = 4;
    public int BatchSize { get; set; } = 10;
}