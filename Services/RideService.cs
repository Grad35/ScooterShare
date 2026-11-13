using System.Data;
using Microsoft.EntityFrameworkCore;
using ScooterShare.Data;
using ScooterShare.Models;

namespace ScooterShare.Services;

public class RideService
{
    public const decimal MinimumBalance = 100m;

    public AppUser GetUser(int id)
    {
        using var db = new AppDbContext();
        return db.Users.AsNoTracking().First(x => x.Id == id);
    }

    public List<Scooter> GetScooters()
    {
        using var db = new AppDbContext();
        return db.Scooters.AsNoTracking().OrderByDescending(x => x.IsAvailable).ThenBy(x => x.Number).ToList();
    }

    public Ride? GetActiveRide(int userId)
    {
        using var db = new AppDbContext();
        return db.Rides.Include(x => x.Scooter).Include(x => x.Tariff)
            .AsNoTracking().FirstOrDefault(x => x.UserId == userId && x.Status == "Active");
    }

    public void StartRide(int userId, int scooterId, int tariffId)
    {
        using var db = new AppDbContext();
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        var user = db.Users.Single(x => x.Id == userId);
        if (user.Balance < MinimumBalance)
            throw new InvalidOperationException($"Для начала поездки на балансе должно быть не менее {MinimumBalance:N0} ₽.");
        if (db.Rides.Any(x => x.UserId == userId && x.Status == "Active"))
            throw new InvalidOperationException("У вас уже есть активная поездка.");
        var scooter = db.Scooters.Single(x => x.Id == scooterId);
        if (!scooter.IsAvailable) throw new InvalidOperationException("Этот самокат уже занят.");
        if (scooter.BatteryPercent < 15) throw new InvalidOperationException("Недостаточный заряд самоката.");
        var tariff = db.Tariffs.Single(x => x.Id == tariffId && x.ValidFrom <= DateTime.Now && (x.ValidTo == null || x.ValidTo >= DateTime.Now));
        scooter.IsAvailable = false;
        db.Rides.Add(new Ride { UserId = userId, ScooterId = scooterId, TariffId = tariff.Id, StartedAt = DateTime.Now, Status = "Active" });
        db.SaveChanges();
        tx.Commit();
    }

    public decimal FinishRide(int userId)
    {
        using var db = new AppDbContext();
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        var ride = db.Rides.Include(x => x.Tariff).Include(x => x.Scooter)
            .SingleOrDefault(x => x.UserId == userId && x.Status == "Active")
            ?? throw new InvalidOperationException("Активная поездка не найдена.");
        var user = db.Users.Single(x => x.Id == userId);
        var finish = DateTime.Now;
        int minutes = Math.Max(1, (int)Math.Ceiling((finish - ride.StartedAt).TotalMinutes));
        decimal cost = Math.Round(ride.Tariff!.UnlockFee + minutes * ride.Tariff.PricePerMinute, 2);
        if (user.Balance < cost)
            throw new InvalidOperationException($"Недостаточно средств. Стоимость поездки: {cost:N2} ₽, баланс: {user.Balance:N2} ₽. Пополните баланс и завершите поездку.");
        ride.FinishedAt = finish;
        ride.TotalCost = cost;
        ride.Status = "Completed";
        user.Balance -= cost;
        ride.Scooter!.IsAvailable = true;
        db.Transactions.Add(new WalletTransaction { UserId = userId, RideId = ride.Id, Amount = -cost, Type = "Ride", Description = $"Поездка {ride.Scooter.Number}, {minutes} мин." });
        db.SaveChanges();
        tx.Commit();
        return cost;
    }

    public void TopUp(int userId, decimal amount)
    {
        if (amount <= 0 || amount > 100000) throw new ArgumentException("Сумма пополнения должна быть от 1 до 100 000 ₽.");
        using var db = new AppDbContext();
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        var user = db.Users.Single(x => x.Id == userId);
        user.Balance += amount;
        db.Transactions.Add(new WalletTransaction { UserId = userId, Amount = amount, Type = "TopUp", Description = "Пополнение баланса" });
        db.SaveChanges();
        tx.Commit();
    }

    public List<object[]> GetHistory(int userId)
    {
        using var db = new AppDbContext();
        return db.Rides.Include(x => x.Scooter).Include(x => x.Tariff).Where(x => x.UserId == userId)
            .OrderByDescending(x => x.StartedAt).AsEnumerable()
            .Select(x => new object[] { x.StartedAt.ToString("dd.MM.yyyy HH:mm"), x.Scooter!.Number, x.Tariff!.Name,
                x.FinishedAt == null ? "В процессе" : x.FinishedAt.Value.ToString("dd.MM.yyyy HH:mm"),
                x.TotalCost.ToString("N2") + " ₽", x.Status == "Active" ? "Активна" : "Завершена" }).ToList();
    }

    public List<object[]> GetTransactions(int userId)
    {
        using var db = new AppDbContext();
        return db.Transactions.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).AsEnumerable()
            .Select(x => new object[] { x.CreatedAt.ToString("dd.MM.yyyy HH:mm"), x.Type, x.Description, x.Amount.ToString("N2") + " ₽" }).ToList();
    }

    public (decimal Revenue, int Rides, decimal Average, List<object[]> Days) GetReport()
    {
        using var db = new AppDbContext();
        var completed = db.Rides.Where(x => x.Status == "Completed").ToList();
        decimal revenue = completed.Sum(x => x.TotalCost);
        decimal avg = completed.Count == 0 ? 0 : revenue / completed.Count;
        var days = completed.GroupBy(x => x.StartedAt.Date).OrderByDescending(g => g.Key).Take(14)
            .Select(g => new object[] { g.Key.ToString("dd.MM.yyyy"), g.Count(), g.Sum(x => x.TotalCost).ToString("N2") + " ₽" }).ToList();
        return (revenue, completed.Count, avg, days);
    }

    public List<Tariff> GetTariffs()
    {
        using var db = new AppDbContext();
        return db.Tariffs.AsNoTracking().Where(x => x.ValidTo == null || x.ValidTo >= DateTime.Now).OrderBy(x => x.Id).ToList();
    }
}
