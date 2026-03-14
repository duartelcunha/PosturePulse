using PosturePulse.Helpers;
using PosturePulse.Models;
using PosturePulse.Services;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace PosturePulse.Views;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ReminderScheduler _scheduler;
    private readonly DispatcherTimer _statusTimer;

    public bool AllowClose { get; set; }

    public MainWindow(AppSettings settings, ReminderScheduler scheduler)
    {
        InitializeComponent();
        _settings = settings;
        _scheduler = scheduler;
        DataContext = settings;

        // Apply Mica backdrop once the window handle exists.
        Loaded += OnLoaded;
        StateChanged += OnStateChanged;

        // Refresh countdown/snooze status every 15 seconds.
        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _statusTimer.Tick += (_, _) => RefreshStatus();
        _statusTimer.Start();
        RefreshStatus();

        settings.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(AppSettings.SnoozedUntilUtc):
                case nameof(AppSettings.PostureSnoozedUntilUtc):
                case nameof(AppSettings.WaterSnoozedUntilUtc):
                case nameof(AppSettings.QuietHoursEnabled):
                case nameof(AppSettings.QuietHoursStartHour):
                case nameof(AppSettings.QuietHoursEndHour):
                    RefreshStatus();
                    break;
            }
        };
    }

    // ── Mica / DWM backdrop ────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            bool micaApplied = BackdropHelper.ApplyMica(this, useMicaAlt: true);

            if (micaApplied)
            {
                // When Mica is active, make the root border mostly transparent
                // so the system backdrop shines through.
                RootBorder.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(0xC0, 0x08, 0x0D, 0x15));
            }
            else
            {
                // Fallback: solid dark background on older Windows versions.
                RootBorder.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x0C, 0x0F, 0x14));
            }
        }
        catch
        {
            // DWM interop can fail; fall back gracefully.
            RootBorder.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x0C, 0x0F, 0x14));
        }
    }

    // ── Window state ───────────────────────────────────────────────────────

    private void OnStateChanged(object? sender, EventArgs e)
    {
        // Update the maximize/restore glyph.
        MaxRestoreBtn.Content = WindowState == WindowState.Maximized
            ? "\uE923"   // Restore glyph
            : "\uE922";  // Maximize glyph
    }

    // ── Caption button handlers ────────────────────────────────────────────

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaxRestore_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();

    // ── Status display ──────────────────────────────────────────────────────

    private void RefreshStatus()
    {
        var now = DateTime.UtcNow;

        // Global snooze banner
        if (_settings.SnoozedUntilUtc.HasValue && now < _settings.SnoozedUntilUtc.Value)
        {
            var remaining = _settings.SnoozedUntilUtc.Value - now;
            SnoozeBannerText.Text = $"All reminders paused for {FormatTimeSpan(remaining)}";
            SnoozeBanner.Visibility = Visibility.Visible;
        }
        else
        {
            SnoozeBanner.Visibility = Visibility.Collapsed;
        }

        // Posture countdown
        var postureRemaining = _scheduler.TimeUntilPosture();
        PostureCountdownText.Text = postureRemaining.HasValue
            ? $"Next in {FormatTimeSpan(postureRemaining.Value)}"
            : string.Empty;

        // Water countdown
        var waterRemaining = _scheduler.TimeUntilWater();
        WaterCountdownText.Text = waterRemaining.HasValue
            ? $"Next in {FormatTimeSpan(waterRemaining.Value)}"
            : string.Empty;

        // Quiet hours hint
        if (_settings.QuietHoursEnabled)
        {
            var start = _settings.QuietHoursStartHour;
            var end = _settings.QuietHoursEndHour;
            QuietHoursHint.Text = $"Reminders suppressed {start:D2}:00 → {end:D2}:00 (local time)";
        }
        else
        {
            QuietHoursHint.Text = string.Empty;
        }
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts <= TimeSpan.Zero) return "now";
        if (ts.TotalMinutes < 1) return "< 1 min";
        if (ts.TotalHours < 1) return $"{(int)ts.TotalMinutes} min";
        var h = (int)ts.TotalHours;
        var m = ts.Minutes;
        return m == 0 ? $"{h} h" : $"{h} h {m} min";
    }

    // ── Button handlers ─────────────────────────────────────────────────────

    private void TestPosture_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.TriggerReminder(ReminderKind.Posture);
    }

    private void TestWater_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.TriggerReminder(ReminderKind.Water);
    }

    private void HideToTray_Click(object sender, RoutedEventArgs e) => Hide();

    private void Pause15_Click(object sender, RoutedEventArgs e)
    {
        _scheduler.Snooze(TimeSpan.FromMinutes(15));
        RefreshStatus();
    }

    private void Pause30_Click(object sender, RoutedEventArgs e)
    {
        _scheduler.Snooze(TimeSpan.FromMinutes(30));
        RefreshStatus();
    }

    private void Pause60_Click(object sender, RoutedEventArgs e)
    {
        _scheduler.Snooze(TimeSpan.FromHours(1));
        RefreshStatus();
    }

    private void Resume_Click(object sender, RoutedEventArgs e)
    {
        _scheduler.Resume();
        RefreshStatus();
    }

    // ── Window lifecycle ───────────────────────────────────────────────────

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _statusTimer.Stop();
        base.OnClosing(e);
    }
}
