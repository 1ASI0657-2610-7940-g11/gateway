using Fuel.Orders.Service.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Orders.Service.Infrastructure.Data;

public sealed class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(32);
            entity.Property(x => x.UserId).HasMaxLength(32);
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(20);
            entity.Property(x => x.Product).HasMaxLength(100);
            entity.Property(x => x.Eta).HasMaxLength(200);
            entity.Property(x => x.Plant).HasMaxLength(160);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.TimeWindow).HasMaxLength(100);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.PaymentMethod).HasMaxLength(120);
            entity.Property(x => x.Amount).HasPrecision(14, 2);
            entity.Property(x => x.VehicleId).HasMaxLength(60);
            entity.Property(x => x.VehiclePlate).HasMaxLength(30);
            entity.Property(x => x.DriverName).HasMaxLength(160);
            entity.Property(x => x.LastStatusComment).HasMaxLength(500);
            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        });
    }
}