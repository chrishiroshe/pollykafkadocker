using Microsoft.EntityFrameworkCore;
using OrderSystem.Application.Abstractions;
using OrderSystem.Infrastructure.Persistence;
using OrderSystem.Domain.Entities;

namespace OrderSystem.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task AddAsync( OrderSystem.Domain.Entities.Order order, CancellationToken ct)
    {
        _db.Orders.Add(order);
        return Task.CompletedTask;
    }

    public Task<OrderSystem.Domain.Entities.Order?> GetAsync(Guid id, CancellationToken ct)
        => _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => _db.SaveChangesAsync(ct);
}