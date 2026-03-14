using PosturePulse.Models;
using PosturePulse.Services;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Color = System.Windows.Media.Color;

namespace PosturePulse.Views;

public partial class ReminderPopup : Window
{
    private readonly ReminderKind _kind;
    private readonly AppSettings _settings;
    private readonly ReminderScheduler _scheduler;
    private readonly Screen _screen;

    public ReminderPopup(ReminderKind kind, AppSettings settings, ReminderScheduler scheduler, Screen screen)
    {
        InitializeComponent();
        _kind = kind;
        _settings = settings;
        _scheduler = scheduler;
        _screen = screen;
        ApplyContent(kind);
        Loaded += ReminderPopup_Loaded;
    }

    public static void ShowOnAllScreens(ReminderKind kind, AppSettings settings, ReminderScheduler scheduler)
    {
        foreach (var screen in Screen.AllScreens)
        {
            var popup = new ReminderPopup(kind, settings, scheduler, screen);
            popup.Show();
        }
    }

    private void ApplyContent(ReminderKind kind)
    {
        if (kind == ReminderKind.Posture)
        {
            TitleText.Text = "POSTURE REMINDER";
            MessageText.Text = _settings.PostureMessage;
            IconPath.Data = (Geometry)FindResource("IconPosture");
            IconFill.Color = Color.FromRgb(0x80, 0xBB, 0xFF);
            GradStop1.Color = Color.FromRgb(0x1A, 0x35, 0x60);
            GradStop2.Color = Color.FromRgb(0x1C, 0x50, 0x90);
        }
        else
        {
            TitleText.Text = "WATER REMINDER";
            MessageText.Text = _settings.WaterMessage;
            IconPath.Data = (Geometry)FindResource("IconWater");
            IconFill.Color = Color.FromRgb(0x56, 0xD4, 0xE0);
            GradStop1.Color = Color.FromRgb(0x0E, 0x30, 0x40);
            GradStop2.Color = Color.FromRgb(0x10, 0x50, 0x60);
        }
    }

    private void ReminderPopup_Loaded(object sender, RoutedEventArgs e)
    {
        PositionWindow();
        BeginOpenAnimation();
        StartAutoCloseTimer();
    }

    private void PositionWindow()
    {
        var area = _screen.WorkingArea;
        var dpi = VisualTreeHelper.GetDpi(this);
        double dipRight = area.Right / dpi.DpiScaleX;
        double dipTop = area.Top / dpi.DpiScaleY;

        Left = dipRight - Width - 20;
        Top = dipTop + 20;
    }

    private void BeginOpenAnimation()
    {
        Opacity = 0;

        BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });

        PopupTranslate.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation(-12, 0, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private void StartAutoCloseTimer()
    {
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Max(2, _settings.PopupDurationSeconds))
        };

        timer.Tick += (_, _) =>
        {
            timer.Stop();
            CloseWithFade();
        };

        timer.Start();
    }

    private void CloseWithFade()
    {
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
    }

    // ── Button handlers ─────────────────────────────────────────────────────

    private void Dismiss_Click(object sender, RoutedEventArgs e) => CloseWithFade();

    private void Snooze5_Click(object sender, RoutedEventArgs e) => ApplySnooze(TimeSpan.FromMinutes(5));

    private void Snooze15_Click(object sender, RoutedEventArgs e) => ApplySnooze(TimeSpan.FromMinutes(15));

    private void ApplySnooze(TimeSpan duration)
    {
        if (_kind == ReminderKind.Posture)
            _scheduler.SnoozePosture(duration);
        else
            _scheduler.SnoozeWater(duration);

        CloseWithFade();
    }
}
