
using ScooterShare.Data;
using ScooterShare.Models;
using ScooterShare.Services;

namespace ScooterShare.UI;

public class AuthForm : Form
{
    private readonly TextBox email = new()
    {
        Width = 260,
        Margin = new Padding(0, 3, 0, 8)
    };

    private readonly TextBox password = new()
    {
        Width = 260,
        UseSystemPasswordChar = true,
        Margin = new Padding(0, 3, 0, 8)
    };

    private readonly TextBox name = new()
    {
        Width = 260,
        Margin = new Padding(0, 3, 0, 8)
    };

    private readonly Label nameLabel = CreateLabel("Имя");
    private readonly Label emailLabel = CreateLabel("Электронная почта");
    private readonly Label passwordLabel = CreateLabel("Пароль");

    private readonly Button action = new()
    {
        Text = "Войти",
        Width = 260,
        Height = 38,
        BackColor = Color.FromArgb(32, 120, 91),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Margin = new Padding(0, 15, 0, 5),
        Cursor = Cursors.Hand
    };

    private readonly LinkLabel switchMode = new()
    {
        Text = "Нет аккаунта? Зарегистрироваться",
        AutoSize = true,
        LinkColor = Color.FromArgb(32, 120, 91),
        Margin = new Padding(0, 8, 0, 0)
    };

    private readonly TableLayoutPanel layout = new()
    {
        ColumnCount = 1,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        Padding = new Padding(28),
        BackColor = Color.White
    };

    private bool registerMode;

    public AppUser? AuthenticatedUser { get; private set; }

    private static Label CreateLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(55, 65, 75),
            Margin = new Padding(0, 8, 0, 2)
        };
    }

    public AuthForm()
    {
        Text = "ScooterShare — вход";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        BackColor = Color.FromArgb(247, 249, 248);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        layout.RowCount = 9;
        layout.Dock = DockStyle.Fill;

        var title = new Label
        {
            Text = "SCOOTERSHARE",
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 120, 91),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        // Строгое расположение элементов по строкам

        layout.Controls.Add(title, 0, 0);

        layout.Controls.Add(nameLabel, 0, 1);
        layout.Controls.Add(name, 0, 2);

        layout.Controls.Add(emailLabel, 0, 3);
        layout.Controls.Add(email, 0, 4);

        layout.Controls.Add(passwordLabel, 0, 5);
        layout.Controls.Add(password, 0, 6);

        layout.Controls.Add(action, 0, 7);
        layout.Controls.Add(switchMode, 0, 8);

        nameLabel.Visible = false;
        name.Visible = false;

        Controls.Add(layout);

        action.Click += (_, _) => Submit();
        switchMode.Click += (_, _) => ToggleMode();

        AcceptButton = action;

        Shown += (_, _) => email.Focus();
    }

    private void ToggleMode()
    {
        registerMode = !registerMode;

        nameLabel.Visible = registerMode;
        name.Visible = registerMode;

        action.Text = registerMode
            ? "Создать аккаунт"
            : "Войти";

        switchMode.Text = registerMode
            ? "Уже есть аккаунт? Войти"
            : "Нет аккаунта? Зарегистрироваться";

        Text = registerMode
            ? "ScooterShare — регистрация"
            : "ScooterShare — вход";

        layout.PerformLayout();
        PerformLayout();
    }

    private void Submit()
    {
        var mail = email.Text.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(mail) ||
            !mail.Contains('@') ||
            password.Text.Length < 6)
        {
            MessageBox.Show(
                "Введите корректную почту и пароль не короче 6 символов.",
                "Проверка данных");
            return;
        }

        try
        {
            using var db = new AppDbContext();

            if (registerMode)
            {
                if (string.IsNullOrWhiteSpace(name.Text))
                {
                    MessageBox.Show("Введите имя.");
                    return;
                }

                if (db.Users.Any(x => x.Email == mail))
                {
                    MessageBox.Show(
                        "Пользователь с такой почтой уже существует.");
                    return;
                }

                var user = new AppUser
                {
                    FullName = name.Text.Trim(),
                    Email = mail,
                    PasswordHash = PasswordService.Hash(password.Text),
                    Balance = 0
                };

                db.Users.Add(user);
                db.SaveChanges();

                AuthenticatedUser = user;
            }
            else
            {
                var user = db.Users.SingleOrDefault(
                    x => x.Email == mail);

                if (user is null ||
                    !PasswordService.Verify(password.Text, user.PasswordHash))
                {
                    MessageBox.Show("Неверная почта или пароль.");
                    return;
                }

                AuthenticatedUser = user;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
