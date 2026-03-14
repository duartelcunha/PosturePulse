using Microsoft.Win32;
using PosturePulse.Helpers;
using PosturePulse.Models;
using PosturePulse.Services;
using PosturePulse.Views;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using WinForms = System.Windows.Forms;

namespace PosturePulse;

public partial class App : System.Windows.Application
{
    private WinForms.NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private SettingsService? _settingsService;
    private ReminderScheduler? _scheduler;
    private SoundService? _soundService;
    private Icon? _appIcon;

    public AppSettings Settings { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _settingsService = new SettingsService();
        Settings = _settingsService.Load();
        ApplyStartupRegistration(Settings.StartWithWindows);

        Settings.PropertyChanged += Settings_PropertyChanged;

        _soundService = new SoundService();
        _scheduler = new ReminderScheduler(Settings, PersistSettings);
        _scheduler.ReminderDue += Scheduler_ReminderDue;
        _scheduler.Start();

        // Build the multi-size icon once for both window and tray.
        _appIcon = IconBuilder.BuildMultiSizeIcon();

        _mainWindow = new MainWindow(Settings, _scheduler);
        _mainWindow.Icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
            _appIcon.Handle,
            Int32Rect.Empty,
            System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        MainWindow = _mainWindow;
        _mainWindow.Show();

        CreateTrayIcon();
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        PersistSettings();

        if (e.PropertyName == nameof(AppSettings.StartWithWindows))
            ApplyStartupRegistration(Settings.StartWithWindows);
    }

    private void Scheduler_ReminderDue(object? sender, ReminderEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            if (Settings.SoundEnabled)
                _soundService?.Play(Settings.SoundVolume);

            ReminderPopup.ShowOnAllScreens(e.Kind, Settings, _scheduler!);
            PersistSettings();
        });
    }

    private void PersistSettings() => _settingsService?.Save(Settings);

    // ── Tray icon ─────────────────────────────────────────────────────────

    private void CreateTrayIcon()
    {
        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = _appIcon ?? IconBuilder.BuildSingleIcon(16),
            Text = "PosturePulse",
            Visible = true,
            ContextMenuStrip = new WinForms.ContextMenuStrip()
        };

        var menu = _notifyIcon.ContextMenuStrip;

        // Style the tray context menu for a modern dark look.
        menu.Renderer = new DarkMenuRenderer();

        menu.Items.Add("Open settings", null, (_, _) => ShowSettings());
        menu.Items.Add("Test posture", null, (_, _) => TriggerReminder(ReminderKind.Posture));
        menu.Items.Add("Test water", null, (_, _) => TriggerReminder(ReminderKind.Water));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Pause 15 min", null, (_, _) => _scheduler?.Snooze(TimeSpan.FromMinutes(15)));
        menu.Items.Add("Pause 30 min", null, (_, _) => _scheduler?.Snooze(TimeSpan.FromMinutes(30)));
        menu.Items.Add("Pause 1 hour", null, (_, _) => _scheduler?.Snooze(TimeSpan.FromHours(1)));
        menu.Items.Add("Resume", null, (_, _) => _scheduler?.Resume());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => QuitApp());

        _notifyIcon.DoubleClick += (_, _) => ShowSettings();
    }

    // ── Public surface ─────────────────────────────────────────────────────

    public void ShowSettings()
    {
        if (_mainWindow is null) return;

        if (!_mainWindow.IsVisible)
            _mainWindow.Show();

        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;

        _mainWindow.Activate();
    }

    public void TriggerReminder(ReminderKind kind)
    {
        if (Settings.SoundEnabled)
            _soundService?.Play(Settings.SoundVolume);

        ReminderPopup.ShowOnAllScreens(kind, Settings, _scheduler!);
    }

    public void QuitApp()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _scheduler?.Stop();
        _scheduler?.Dispose();
        PersistSettings();

        if (_mainWindow is not null)
        {
            _mainWindow.AllowClose = true;
            _mainWindow.Close();
        }

        _appIcon?.Dispose();
        Shutdown();
    }

    // ── Startup registration ──────────────────────────────────────────────

    private static void ApplyStartupRegistration(bool enabled)
    {
        try
        {
            const string runKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string appName = "PosturePulse";

            using var key = Registry.CurrentUser.OpenSubKey(runKey, writable: true)
                         ?? Registry.CurrentUser.CreateSubKey(runKey);

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath)) return;

            if (enabled)
                key?.SetValue(appName, $"\"{exePath}\"");
            else
                key?.DeleteValue(appName, throwOnMissingValue: false);
        }
        catch
        {
            // Silently ignore; startup registration is non-critical.
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _scheduler?.Dispose();
        _notifyIcon?.Dispose();
        _appIcon?.Dispose();
        base.OnExit(e);
    }
}

/// <summary>Minimal P/Invoke for GDI icon handle cleanup.</summary>
internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr hIcon);
}

/// <summary>
/// Dark-themed renderer for the system tray context menu.
/// Makes the right-click menu match the app's dark aesthetic.
/// </summary>
internal class DarkMenuRenderer : WinForms.ToolStripProfessionalRenderer
{
    private static readonly Color MenuBg = Color.FromArgb(20, 24, 32);
    private static readonly Color MenuBorder = Color.FromArgb(38, 50, 68);
    private static readonly Color ItemHover = Color.FromArgb(30, 38, 52);
    private static readonly Color TextColor = Color.FromArgb(200, 210, 225);
    private static readonly Color SepColor = Color.FromArgb(30, 40, 55);

    public DarkMenuRenderer() : base(new DarkMenuColorTable()) { }

    protected override void OnRenderMenuItemBackground(WinForms.ToolStripItemRenderEventArgs e)
    {
        var rect = new Rectangle(System.Drawing.Point.Empty, e.Item.Size);

        if (e.Item.Selected || e.Item.Pressed)
        {
            using var brush = new SolidBrush(ItemHover);
            e.Graphics.FillRectangle(brush, rect);
        }
        else
        {
            using var brush = new SolidBrush(MenuBg);
            e.Graphics.FillRectangle(brush, rect);
        }
    }

    protected override void OnRenderItemText(WinForms.ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = TextColor;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(WinForms.ToolStripSeparatorRenderEventArgs e)
    {
        int y = e.Item.Height / 2;
        using var pen = new Pen(SepColor);
        e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
    }

    protected override void OnRenderToolStripBackground(WinForms.ToolStripRenderEventArgs e)
    {
        using var brush = new SolidBrush(MenuBg);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(WinForms.ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(MenuBorder);
        var rect = new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
        e.Graphics.DrawRectangle(pen, rect);
    }

    private class DarkMenuColorTable : WinForms.ProfessionalColorTable
    {
        public override Color MenuBorder => Color.FromArgb(38, 50, 68);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuItemSelected => Color.FromArgb(30, 38, 52);
        public override Color MenuStripGradientBegin => Color.FromArgb(20, 24, 32);
        public override Color MenuStripGradientEnd => Color.FromArgb(20, 24, 32);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(30, 38, 52);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(30, 38, 52);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(25, 32, 44);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(25, 32, 44);
        public override Color ImageMarginGradientBegin => Color.FromArgb(20, 24, 32);
        public override Color ImageMarginGradientEnd => Color.FromArgb(20, 24, 32);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(20, 24, 32);
        public override Color ToolStripDropDownBackground => Color.FromArgb(20, 24, 32);
        public override Color SeparatorDark => Color.FromArgb(30, 40, 55);
        public override Color SeparatorLight => Color.Transparent;
    }
}
