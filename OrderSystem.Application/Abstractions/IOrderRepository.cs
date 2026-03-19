namespace OrderSystem.Application.Abstractions
{
    using OrderSystem.Domain.Entities;
    public interface IOrderRepository
    {
        Task AddAsync(Order order, CancellationToken ct);
     
        Task<Order?> GetAsync(Guid id, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}
