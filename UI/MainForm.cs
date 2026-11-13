using ScooterShare.Data;
using ScooterShare.Services;

namespace ScooterShare.UI;

public class MainForm : Form
{
    private readonly int userId;
    private readonly RideService service = new();
    private readonly Label balanceLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
    private readonly Label activeLabel = new() { AutoSize = true };
    private readonly DataGridView scootersGrid = Grid();
    private readonly DataGridView historyGrid = Grid();
    private readonly DataGridView transactionsGrid = Grid();
    private readonly DataGridView reportGrid = Grid();
    private readonly ComboBox tariffBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly NumericUpDown topUpAmount = new() { Minimum = 100, Maximum = 100000, Increment = 100, Value = 500, Width = 130 };
    private readonly Label reportSummary = new() { AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold) };

    public MainForm(int id)
    {
        userId = id;
        Text = "ScooterShare — аренда электросамокатов";
        Width = 1120; Height = 760; MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildScootersTab());
        tabs.TabPages.Add(BuildRideTab());
        tabs.TabPages.Add(BuildHistoryTab());
        tabs.TabPages.Add(BuildWalletTab());
        tabs.TabPages.Add(BuildReportsTab());
        Controls.Add(tabs);
        RefreshAll();
        var timer = new System.Windows.Forms.Timer { Interval = 15000 };
        timer.Tick += (_, _) => RefreshAll();
        timer.Start();
        FormClosed += (_, _) => timer.Dispose();
    }

    private static DataGridView Grid()
    {
        return new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            BackgroundColor = Color.White, RowHeadersVisible = false };
    }

    private TabPage BuildScootersTab()
    {
        var page = new TabPage("Самокаты");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(12), WrapContents = false };
        top.Controls.Add(new Label { Text = "Доступные самокаты и их заряд", AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold), Margin = new Padding(0, 8, 25, 0) });
        var refresh = new Button { Text = "Обновить", AutoSize = true };
        refresh.Click += (_, _) => RefreshScooters();
        top.Controls.Add(refresh);
        page.Controls.Add(scootersGrid); page.Controls.Add(top);
        return page;
    }

    private TabPage BuildRideTab()
    {
        var page = new TabPage("Поездка");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 210, Padding = new Padding(18), FlowDirection = FlowDirection.TopDown, WrapContents = false };
        panel.Controls.Add(new Label { Text = "Управление поездкой", Font = new Font("Segoe UI", 15, FontStyle.Bold), AutoSize = true });
        panel.Controls.Add(activeLabel);
        panel.Controls.Add(new Label { Text = "Тариф для новой поездки:", AutoSize = true });
        panel.Controls.Add(tariffBox);
        var start = new Button { Text = "Начать поездку на выбранном самокате", Width = 300, Height = 38 };
        start.Click += (_, _) => StartRide();
        var finish = new Button { Text = "Завершить поездку", Width = 220, Height = 38 };
        finish.Click += (_, _) => FinishRide();
        panel.Controls.Add(start); panel.Controls.Add(finish);
        panel.Controls.Add(new Label { Text = "Начало поездки блокирует самокат. Стоимость = разблокировка + минуты × цена тарифа.", AutoSize = true });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildHistoryTab()
    {
        var page = new TabPage("История");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 330 };
        split.Panel1.Controls.Add(historyGrid); split.Panel2.Controls.Add(transactionsGrid);
        page.Controls.Add(split);
        return page;
    }

    private TabPage BuildWalletTab()
    {
        var page = new TabPage("Баланс");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 160, Padding = new Padding(20), FlowDirection = FlowDirection.TopDown };
        panel.Controls.Add(balanceLabel);
        panel.Controls.Add(new Label { Text = "Демо-пополнение баланса (без реального платежа):", AutoSize = true });
        var row = new FlowLayoutPanel { AutoSize = true };
        row.Controls.Add(topUpAmount);
        var button = new Button { Text = "Пополнить", AutoSize = true };
        button.Click += (_, _) => TopUp();
        row.Controls.Add(button); panel.Controls.Add(row);
        panel.Controls.Add(new Label { Text = "Минимальный баланс для старта поездки: 100 ₽.", AutoSize = true });
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildReportsTab()
    {
        var page = new TabPage("Отчёты");
        var panel = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(12) };
        panel.Controls.Add(reportSummary);
        page.Controls.Add(reportGrid); page.Controls.Add(panel);
        return page;
    }

    private void RefreshAll()
    {
        try
        {
            var user = service.GetUser(userId);
            balanceLabel.Text = $"Пользователь: {user.FullName}   |   Баланс: {user.Balance:N2} ₽";
            RefreshScooters();
            var active = service.GetActiveRide(userId);
            activeLabel.Text = active is null ? "Активной поездки нет." :
                $"Активная поездка: {active.Scooter!.Number}, тариф «{active.Tariff!.Name}», начата {active.StartedAt:dd.MM.yyyy HH:mm}.";
            if (tariffBox.Items.Count == 0)
            {
                foreach (var t in service.GetTariffs()) tariffBox.Items.Add(new TariffChoice(t.Id, $"{t.Name} — {t.UnlockFee:N0} ₽ + {t.PricePerMinute:N0} ₽/мин"));
                if (tariffBox.Items.Count > 0) tariffBox.SelectedIndex = 0;
            }
            FillGrid(historyGrid, new[] { "Начало", "Самокат", "Тариф", "Окончание", "Стоимость", "Статус" }, service.GetHistory(userId));
            FillGrid(transactionsGrid, new[] { "Дата", "Тип", "Описание", "Сумма" }, service.GetTransactions(userId));
            var report = service.GetReport();
            reportSummary.Text = $"Всего поездок: {report.Rides}    Выручка: {report.Revenue:N2} ₽    Средний чек: {report.Average:N2} ₽";
            FillGrid(reportGrid, new[] { "День", "Поездок", "Выручка" }, report.Days);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка обновления"); }
    }

    private void RefreshScooters()
    {
        var rows = service.GetScooters().Select(s => new object[] { s.Number, s.Model, s.BatteryPercent + "%", s.Location, s.IsAvailable ? "Свободен" : "Занят" }).ToList();
        FillGrid(scootersGrid, new[] { "Номер", "Модель", "Заряд", "Местоположение", "Статус" }, rows);
    }

    private static void FillGrid(DataGridView grid, string[] headers, List<object[]> rows)
    {
        grid.Columns.Clear();
        foreach (var header in headers) grid.Columns.Add(header, header);
        foreach (var row in rows) grid.Rows.Add(row);
    }

    private void StartRide()
    {
        if (scootersGrid.CurrentRow is null || tariffBox.SelectedItem is not TariffChoice choice)
        { MessageBox.Show("Выберите самокат во вкладке «Самокаты» и тариф."); return; }
        string number = Convert.ToString(scootersGrid.CurrentRow.Cells[0].Value) ?? "";
        if (Convert.ToString(scootersGrid.CurrentRow.Cells[4].Value) != "Свободен")
        { MessageBox.Show("Выбранный самокат занят."); return; }
        try
        {
            using var db = new AppDbContext();
            int scooterId = db.Scooters.Where(x => x.Number == number).Select(x => x.Id).Single();
            service.StartRide(userId, scooterId, choice.Id);
            MessageBox.Show("Поездка началась! Самокат забронирован за вами.");
            RefreshAll();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Не удалось начать поездку"); }
    }

    private void FinishRide()
    {
        try
        {
            var cost = service.FinishRide(userId);
            MessageBox.Show($"Поездка завершена. Списано {cost:N2} ₽.");
            RefreshAll();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Не удалось завершить поездку"); }
    }

    private void TopUp()
    {
        try
        {
            service.TopUp(userId, topUpAmount.Value);
            RefreshAll();
            MessageBox.Show("Баланс пополнен (демо-операция).");
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
    }

    private sealed record TariffChoice(int Id, string Caption)
    {
        public override string ToString() => Caption;
    }
}
