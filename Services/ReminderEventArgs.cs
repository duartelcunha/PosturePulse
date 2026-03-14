namespace PosturePulse.Services;

public class ReminderEventArgs : EventArgs
{
    public ReminderEventArgs(ReminderKind kind)
    {
        Kind = kind;
    }

    public ReminderKind Kind { get; }
}
