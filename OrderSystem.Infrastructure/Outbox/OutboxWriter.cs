using OrderSystem.Application.Abstractions;
using OrderSystem.Infrastructure.Persistence;

namespace OrderSystem.Infrastructure.Outbox;

public class OutboxWriter : IOutboxWriter
{
    private readonly AppDbContext _db;

    public OutboxWriter(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(string type, string payload, string? traceId, CancellationToken ct)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Type = type,
            Payload = payload,
            TraceId = traceId
        });

        // ⚠️ Não salva aqui. Quem salva é o SaveChangesAsync do repo.
        return Task.CompletedTask;
    }
}