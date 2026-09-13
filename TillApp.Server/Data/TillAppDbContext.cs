using Microsoft.EntityFrameworkCore;
using TillApp.Server.Data.Entities;

namespace TillApp.Server.Data;

public sealed class TillAppDbContext(DbContextOptions<TillAppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TillAppDbContext).Assembly);
    }
}
