using ScooterShare.Data;
using ScooterShare.Services;
using ScooterShare.UI;

namespace ScooterShare;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        using (var db = new AppDbContext())
        {
            db.Database.EnsureCreated();
            SeedData.Initialize(db);
        }

        using var auth = new AuthForm();
        if (auth.ShowDialog() == DialogResult.OK && auth.AuthenticatedUser is not null)
        {
            Application.Run(new MainForm(auth.AuthenticatedUser.Id));
        }
    }
}
