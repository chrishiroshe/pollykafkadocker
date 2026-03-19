using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderSystem.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var cs =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(cs, x => x.MigrationsAssembly("OrderSystem.Infrastructure"));

        return new AppDbContext(optionsBuilder.Options);
    }
    //public AppDbContext CreateDbContext(string[] args)
    //{
    //    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

    //    // ✅ Pega do ambiente se existir, senão usa um default
    //    var cs = Environment.GetEnvironmentVariable("DEFAULT_CONNECTION")
    //             ?? "Host=localhost;Port=5432;Database=ordersdb;Username=postgres;Password=postgres";

    //    optionsBuilder.UseNpgsql(cs, x => x.MigrationsAssembly("OrderSystem.Infrastructure"));

    //    return new AppDbContext(optionsBuilder.Options);
    //}
}