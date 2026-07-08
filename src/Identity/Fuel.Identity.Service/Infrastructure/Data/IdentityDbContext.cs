using Fuel.Identity.Service.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fuel.Identity.Service.Infrastructure.Data;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasMaxLength(32);
            entity.Property(x => x.FullName).HasMaxLength(160);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.PasswordHash).HasMaxLength(255);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<ProfileEntity>(entity =>
        {
            entity.ToTable("profiles");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).HasMaxLength(32);
            entity.Property(x => x.CompanyName).HasMaxLength(200);
            entity.Property(x => x.Ruc).HasMaxLength(20);
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.ContactName).HasMaxLength(160);
            entity.Property(x => x.AvatarContentType).HasMaxLength(100);
            entity.HasOne<UserEntity>()
                .WithOne(u => u.Profile)
                .HasForeignKey<ProfileEntity>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}