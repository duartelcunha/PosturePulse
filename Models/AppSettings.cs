using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PosturePulse.Models;

public class AppSettings : INotifyPropertyChanged
{
    private bool _postureEnabled = true;
    private int _postureIntervalMinutes = 30;
    private string _postureMessage = "Straighten up";
    private bool _waterEnabled = true;
    private int _waterIntervalMinutes = 60;
    private string _waterMessage = "Water break";
    private bool _soundEnabled = true;
    private double _soundVolume = 55;
    private int _popupDurationSeconds = 3;
    private bool _startWithWindows;
    private DateTime _lastPostureReminderUtc = DateTime.UtcNow;
    private DateTime _lastWaterReminderUtc = DateTime.UtcNow;

    // Global snooze (tray menu — pauses everything)
    private DateTime? _snoozedUntilUtc;

    // Per-type snooze (popup button)
    private DateTime? _postureSnoozedUntilUtc;
    private DateTime? _waterSnoozedUntilUtc;

    // Quiet hours
    private bool _quietHoursEnabled;
    private int _quietHoursStartHour = 22;
    private int _quietHoursEndHour = 8;

    public bool PostureEnabled
    {
        get => _postureEnabled;
        set => SetField(ref _postureEnabled, value);
    }

    public int PostureIntervalMinutes
    {
        get => _postureIntervalMinutes;
        set => SetField(ref _postureIntervalMinutes, value);
    }

    public string PostureMessage
    {
        get => _postureMessage;
        set => SetField(ref _postureMessage, value);
    }

    public bool WaterEnabled
    {
        get => _waterEnabled;
        set => SetField(ref _waterEnabled, value);
    }

    public int WaterIntervalMinutes
    {
        get => _waterIntervalMinutes;
        set => SetField(ref _waterIntervalMinutes, value);
    }

    public string WaterMessage
    {
        get => _waterMessage;
        set => SetField(ref _waterMessage, value);
    }

    public bool SoundEnabled
    {
        get => _soundEnabled;
        set => SetField(ref _soundEnabled, value);
    }

    public double SoundVolume
    {
        get => _soundVolume;
        set => SetField(ref _soundVolume, value);
    }

    public int PopupDurationSeconds
    {
        get => _popupDurationSeconds;
        set => SetField(ref _popupDurationSeconds, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetField(ref _startWithWindows, value);
    }

    public DateTime LastPostureReminderUtc
    {
        get => _lastPostureReminderUtc;
        set => SetField(ref _lastPostureReminderUtc, value);
    }

    public DateTime LastWaterReminderUtc
    {
        get => _lastWaterReminderUtc;
        set => SetField(ref _lastWaterReminderUtc, value);
    }

    public DateTime? SnoozedUntilUtc
    {
        get => _snoozedUntilUtc;
        set => SetField(ref _snoozedUntilUtc, value);
    }

    public DateTime? PostureSnoozedUntilUtc
    {
        get => _postureSnoozedUntilUtc;
        set => SetField(ref _postureSnoozedUntilUtc, value);
    }

    public DateTime? WaterSnoozedUntilUtc
    {
        get => _waterSnoozedUntilUtc;
        set => SetField(ref _waterSnoozedUntilUtc, value);
    }

    public bool QuietHoursEnabled
    {
        get => _quietHoursEnabled;
        set => SetField(ref _quietHoursEnabled, value);
    }

    /// <summary>Local hour (0-23) at which quiet hours begin.</summary>
    public int QuietHoursStartHour
    {
        get => _quietHoursStartHour;
        set => SetField(ref _quietHoursStartHour, Math.Clamp(value, 0, 23));
    }

    /// <summary>Local hour (0-23) at which quiet hours end.</summary>
    public int QuietHoursEndHour
    {
        get => _quietHoursEndHour;
        set => SetField(ref _quietHoursEndHour, Math.Clamp(value, 0, 23));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
