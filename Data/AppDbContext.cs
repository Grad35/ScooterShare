using Microsoft.EntityFrameworkCore;
using ScooterShare.Models;

namespace ScooterShare.Data;

public class AppDbContext : DbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Scooter> Scooters => Set<Scooter>();
    public DbSet<Tariff> Tariffs => Set<Tariff>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<WalletTransaction> Transactions => Set<WalletTransaction>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=scootershare.db;Foreign Keys=True");

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        model.Entity<Scooter>().HasIndex(x => x.Number).IsUnique();
        model.Entity<AppUser>().Property(x => x.Balance).HasPrecision(12, 2);
        model.Entity<Tariff>().Property(x => x.PricePerMinute).HasPrecision(8, 2);
        model.Entity<Tariff>().Property(x => x.UnlockFee).HasPrecision(8, 2);
        model.Entity<Ride>().Property(x => x.TotalCost).HasPrecision(12, 2);
        model.Entity<WalletTransaction>().Property(x => x.Amount).HasPrecision(12, 2);
        model.Entity<Ride>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Ride>().HasOne(x => x.Scooter).WithMany().HasForeignKey(x => x.ScooterId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Ride>().HasOne(x => x.Tariff).WithMany().HasForeignKey(x => x.TariffId).OnDelete(DeleteBehavior.Restrict);
    }
}
