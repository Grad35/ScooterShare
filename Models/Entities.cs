namespace ScooterShare.Models;

public class AppUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public decimal Balance { get; set; }
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class Scooter
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Model { get; set; } = "";
    public int BatteryPercent { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string Location { get; set; } = "";
}

public class Tariff
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal PricePerMinute { get; set; }
    public decimal UnlockFee { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public class Ride
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ScooterId { get; set; }
    public int TariffId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = "Active";
    public AppUser? User { get; set; }
    public Scooter? Scooter { get; set; }
    public Tariff? Tariff { get; set; }
}

public class WalletTransaction
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? RideId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
}
