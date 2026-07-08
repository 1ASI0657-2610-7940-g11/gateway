using Fuel.Reporting.Service.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Reporting.Service.Infrastructure.Data;

public sealed class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<DashboardEntity> Dashboards => Set<DashboardEntity>();
    public DbSet<OrderKpiEntity> OrderKpis => Set<OrderKpiEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DashboardEntity>(entity =>
        {
            entity.ToTable("dashboards");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasMaxLength(32);
            entity.Property(x => x.CompanyName).HasMaxLength(200);
            entity.Property(x => x.AvatarUrl).HasMaxLength(200);
            entity.Property(x => x.ActiveOrderFuelType).HasMaxLength(100);
            entity.Property(x => x.ActiveOrderStatus).HasMaxLength(20);
            entity.Property(x => x.NextDeliveryDateTime).HasMaxLength(200);
            entity.Property(x => x.NextDeliveryLocation).HasMaxLength(200);
            entity.Property(x => x.NextDeliveryStatus).HasMaxLength(20);
            entity.Property(x => x.LastPaymentAmount).HasMaxLength(50);
            entity.Property(x => x.LastPaymentMethod).HasMaxLength(40);
            entity.Property(x => x.LastPaymentStatus).HasMaxLength(30);
        });

        modelBuilder.Entity<OrderKpiEntity>(entity =>
        {
            entity.ToTable("order_kpis");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(32);
            entity.Property(x => x.UserId).HasMaxLength(32);
            entity.Property(x => x.OrderCode).HasMaxLength(40);
            entity.Property(x => x.Status).HasMaxLength(20);
            entity.Property(x => x.FuelType).HasMaxLength(100);
            entity.Property(x => x.Amount).HasPrecision(14, 2);
            entity.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        });
    }
}