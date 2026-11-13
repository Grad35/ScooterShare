using ScooterShare.Models;
using ScooterShare.Services;

namespace ScooterShare.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var users = new List<AppUser>
        {
            new() { FullName = "Администратор", Email = "admin@scooter.local", PasswordHash = PasswordService.Hash("admin123"), Balance = 5000, Role = "Admin" },
            new() { FullName = "Иван Петров", Email = "ivan@mail.ru", PasswordHash = PasswordService.Hash("user123"), Balance = 1200 },
            new() { FullName = "Анна Смирнова", Email = "anna@mail.ru", PasswordHash = PasswordService.Hash("user123"), Balance = 850 },
            new() { FullName = "Дмитрий Волков", Email = "dima@mail.ru", PasswordHash = PasswordService.Hash("user123"), Balance = 600 },
            new() { FullName = "Мария Соколова", Email = "maria@mail.ru", PasswordHash = PasswordService.Hash("user123"), Balance = 1000 }
        };
        db.Users.AddRange(users);
        db.Tariffs.AddRange(
            new Tariff { Name = "Стандарт", PricePerMinute = 5, UnlockFee = 20, ValidFrom = DateTime.Today.AddMonths(-2) },
            new Tariff { Name = "Комфорт", PricePerMinute = 8, UnlockFee = 10, ValidFrom = DateTime.Today.AddMonths(-2) });
        db.SaveChanges();

        var scooters = Enumerable.Range(1, 10).Select(i => new Scooter
        {
            Number = $"SC-{i:000}", Model = i % 2 == 0 ? "Xiaomi Mi Scooter" : "Ninebot Max",
            BatteryPercent = 45 + (i * 5 % 55), IsAvailable = true,
            Location = new[] { "Центральная площадь", "Парк", "Вокзал", "Университет", "Торговый центр" }[(i - 1) % 5]
        }).ToList();
        db.Scooters.AddRange(scooters);
        db.SaveChanges();

        var random = new Random(42);
        var tariffIds = db.Tariffs.Select(t => t.Id).ToArray();
        for (int i = 0; i < 100; i++)
        {
            var start = DateTime.Today.AddDays(-random.Next(1, 60)).AddMinutes(random.Next(60, 900));
            int minutes = random.Next(5, 50);
            int userId = users[1 + random.Next(4)].Id;
            int scooterId = scooters[random.Next(scooters.Count)].Id;
            int tariffId = tariffIds[random.Next(tariffIds.Length)];
            var tariff = db.Tariffs.Find(tariffId)!;
            decimal cost = tariff.UnlockFee + minutes * tariff.PricePerMinute;
            db.Rides.Add(new Ride { UserId = userId, ScooterId = scooterId, TariffId = tariffId, StartedAt = start, FinishedAt = start.AddMinutes(minutes), TotalCost = cost, Status = "Completed" });
            db.Transactions.Add(new WalletTransaction { UserId = userId, CreatedAt = start.AddMinutes(minutes), Amount = -cost, Type = "Ride", Description = "Оплата поездки (демо-данные)" });
        }
        db.SaveChanges();
    }
}
