using Fuel.Payments.Service.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Payments.Service.Infrastructure.Data;

public sealed class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<PaymentMethodEntity> PaymentMethods => Set<PaymentMethodEntity>();
    public DbSet<PaymentHistoryEntity> PaymentHistory => Set<PaymentHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PaymentMethodEntity>(entity =>
        {
            entity.ToTable("payment_methods");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(32);
            entity.Property(x => x.UserId).HasMaxLength(32);
            entity.Property(x => x.Brand).HasMaxLength(40);
            entity.Property(x => x.Last4).HasMaxLength(4);
            entity.Property(x => x.Holder).HasMaxLength(160);
            entity.Property(x => x.Expires).HasMaxLength(5);
            entity.HasIndex(x => x.UserId);
        });

        modelBuilder.Entity<PaymentHistoryEntity>(entity =>
        {
            entity.ToTable("payment_history");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(32);
            entity.Property(x => x.UserId).HasMaxLength(32);
            entity.Property(x => x.Description).HasMaxLength(300);
            entity.Property(x => x.Amount).HasPrecision(14, 2);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Status).HasMaxLength(30);
            entity.HasIndex(x => x.UserId);
        });
    }
}