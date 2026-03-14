using PosturePulse.Models;
using System.Windows.Threading;

namespace PosturePulse.Services;

public class ReminderScheduler : IDisposable
{
    private readonly AppSettings _settings;
    private readonly Action _persist;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    public ReminderScheduler(AppSettings settings, Action persist)
    {
        _settings = settings;
        _persist = persist;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _timer.Tick += Timer_Tick;
    }

    public event EventHandler<ReminderEventArgs>? ReminderDue;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    // ── Global snooze (pauses all reminders) ──────────────────────────────

    public void Snooze(TimeSpan duration)
    {
        _settings.SnoozedUntilUtc = DateTime.UtcNow.Add(duration);
        _persist();
    }

    public void Resume()
    {
        _settings.SnoozedUntilUtc = null;
        _settings.PostureSnoozedUntilUtc = null;
        _settings.WaterSnoozedUntilUtc = null;
        _persist();
    }

    // ── Per-type snooze (from popup) ──────────────────────────────────────

    public void SnoozePosture(TimeSpan duration)
    {
        _settings.PostureSnoozedUntilUtc = DateTime.UtcNow.Add(duration);
        _persist();
    }

    public void SnoozeWater(TimeSpan duration)
    {
        _settings.WaterSnoozedUntilUtc = DateTime.UtcNow.Add(duration);
        _persist();
    }

    // ── Countdown helpers (used by status display in MainWindow) ──────────

    /// <summary>
    /// Returns how long until the next posture reminder, accounting for all
    /// active snoozes. Null if posture reminders are disabled.
    /// </summary>
    public TimeSpan? TimeUntilPosture()
    {
        if (!_settings.PostureEnabled) return null;

        var now = DateTime.UtcNow;
        var scheduled = _settings.LastPostureReminderUtc.AddMinutes(_settings.PostureIntervalMinutes);

        var snoozedUntil = MaxNullable(_settings.SnoozedUntilUtc, _settings.PostureSnoozedUntilUtc);
        var effectiveNext = snoozedUntil.HasValue && snoozedUntil.Value > scheduled
            ? snoozedUntil.Value
            : scheduled;

        var remaining = effectiveNext - now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Returns how long until the next water reminder, accounting for all
    /// active snoozes. Null if water reminders are disabled.
    /// </summary>
    public TimeSpan? TimeUntilWater()
    {
        if (!_settings.WaterEnabled) return null;

        var now = DateTime.UtcNow;
        var scheduled = _settings.LastWaterReminderUtc.AddMinutes(_settings.WaterIntervalMinutes);

        var snoozedUntil = MaxNullable(_settings.SnoozedUntilUtc, _settings.WaterSnoozedUntilUtc);
        var effectiveNext = snoozedUntil.HasValue && snoozedUntil.Value > scheduled
            ? snoozedUntil.Value
            : scheduled;

        var remaining = effectiveNext - now;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    // ── Timer tick ────────────────────────────────────────────────────────

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;

        // Global snooze gates everything.
        if (_settings.SnoozedUntilUtc.HasValue && now < _settings.SnoozedUntilUtc.Value)
            return;

        if (IsInQuietHours(now))
            return;

        if (_settings.PostureEnabled && IsPostureDue(now))
        {
            _settings.PostureSnoozedUntilUtc = null;
            _settings.LastPostureReminderUtc = now;
            ReminderDue?.Invoke(this, new ReminderEventArgs(ReminderKind.Posture));
        }

        if (_settings.WaterEnabled && IsWaterDue(now))
        {
            _settings.WaterSnoozedUntilUtc = null;
            _settings.LastWaterReminderUtc = now;
            ReminderDue?.Invoke(this, new ReminderEventArgs(ReminderKind.Water));
        }
    }

    private bool IsPostureDue(DateTime now)
    {
        if (_settings.PostureSnoozedUntilUtc.HasValue && now < _settings.PostureSnoozedUntilUtc.Value)
            return false;

        return now >= _settings.LastPostureReminderUtc.AddMinutes(_settings.PostureIntervalMinutes);
    }

    private bool IsWaterDue(DateTime now)
    {
        if (_settings.WaterSnoozedUntilUtc.HasValue && now < _settings.WaterSnoozedUntilUtc.Value)
            return false;

        return now >= _settings.LastWaterReminderUtc.AddMinutes(_settings.WaterIntervalMinutes);
    }

    /// <summary>
    /// Checks whether the current local time falls inside the configured
    /// quiet-hours window. Handles overnight spans (e.g. 22:00 → 08:00).
    /// </summary>
    private bool IsInQuietHours(DateTime utcNow)
    {
        if (!_settings.QuietHoursEnabled) return false;

        var hour = utcNow.ToLocalTime().Hour;
        var start = _settings.QuietHoursStartHour;
        var end = _settings.QuietHoursEndHour;

        // Same-day span (e.g. 09:00–17:00)
        if (start <= end)
            return hour >= start && hour < end;

        // Overnight span (e.g. 22:00–08:00)
        return hour >= start || hour < end;
    }

    private static DateTime? MaxNullable(DateTime? a, DateTime? b)
    {
        if (a is null) return b;
        if (b is null) return a;
        return a.Value > b.Value ? a : b;
    }

    // ── IDisposable ───────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
    }
}
