namespace OrderSystem.Application.Services;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderSystem.Application.Abstractions;
using OrderSystem.Domain.Entities;
using OrderSystem.Domain.Events;

public class OrdersService
{
    private readonly IOrderRepository _repo;
    private readonly IOutboxWriter _outbox;
    private readonly ILogger<OrdersService> _logger;

    public OrdersService(
        IOrderRepository repo,
        IOutboxWriter outbox,
        ILogger<OrdersService> logger)
    {
        _repo = repo;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<Order> CreateAsync(CancellationToken ct)
    {
        var order = new Order();

        await _repo.AddAsync(order, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} created", order.Id);
        return order;
    }

    public async Task ConfirmAsync(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Starting confirmation for order {OrderId}", id);

        var order = await _repo.GetAsync(id, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            throw new InvalidOperationException("Order not found");
        }

        order.Confirm();

        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        
        var evt = new OrderConfirmedEvent (  order.Id, DateTime.UtcNow, correlationId );

        await _outbox.AddAsync(
            type: "OrderConfirmed",
            payload: JsonSerializer.Serialize(evt),
            traceId: correlationId,
            ct: ct);

        // ✅ agora um save só salva Order + Outbox na mesma transação
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} confirmed and outbox event written", order.Id, correlationId);
    }

    public async Task MarkAsPaidAsync(Guid id, CancellationToken ct)
    {
        var order = await _repo.GetAsync(id, ct);
        if (order is null)
            throw new InvalidOperationException("Order not found");

        order.MarkAsPaid(DateTime.UtcNow);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} marked as paid", order.Id);
    }

    public async Task FailAsync(Guid id, string? reason, CancellationToken ct)
    {
        var order = await _repo.GetAsync(id, ct);
        if (order is null)
            throw new InvalidOperationException("Order not found");

        order.Fail(reason ?? "Unknown");
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Order {OrderId} failed. Reason: {Reason}", order.Id, reason ?? "Unknown");
    }
}