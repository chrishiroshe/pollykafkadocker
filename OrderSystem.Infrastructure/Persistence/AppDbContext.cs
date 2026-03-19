namespace OrderSystem.Infrastructure.Persistence
{
    using Microsoft.EntityFrameworkCore;

    using OrderSystem.Domain.Entities;
    using OrderSystem.Domain.Events;
    using OrderSystem.Infrastructure.Outbox;
    using OrderSystem.Infrastructure.Sagas;

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
        public DbSet<SagaState> SagaStates => Set<SagaState>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>();

            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(x => new { x.ProcessedAtUtc, x.OccurredAtUtc });

            modelBuilder.Entity<OutboxMessage>()
    .HasIndex(x => x.ProcessedAtUtc);

            modelBuilder.Entity<OutboxMessage>()
                .HasIndex(x => x.NextAttemptAtUtc);

            modelBuilder.Entity<ProcessedEvent>()
                .HasKey(x => x.EventId);

            modelBuilder.Entity<SagaState>()
    .HasKey(x => x.OrderId);

            modelBuilder.Entity<SagaState>()
                .Property(x => x.CurrentStep)
                .HasMaxLength(100);

            modelBuilder.Entity<SagaState>()
                .Property(x => x.Status)
                .HasMaxLength(50);
        }
    }
}
