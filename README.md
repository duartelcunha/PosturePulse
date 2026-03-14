# PosturePulse

A lightweight Windows system tray utility that delivers configurable posture correction and hydration reminders throughout your workday.

Built with **WPF + .NET 8**, featuring a modern UI with Windows 11 Mica backdrop, custom chromeless window design, and multi-screen popup support.

## Features

- **Posture & Hydration Reminders** — Customizable intervals with independent timers for each reminder type
- **System Tray Integration** — Runs quietly in the background with a dark-themed context menu
- **Multi-Monitor Popups** — Reminder cards appear across all connected displays simultaneously
- **Mica / Mica-Alt Backdrop** — Native Windows 11 translucency via DWM interop (graceful fallback on older OS)
- **Sound Alerts** — Soft chime notification with adjustable volume
- **Do Not Disturb** — Pause reminders temporarily or schedule quiet hours
- **Start with Windows** — Optional auto-launch via registry integration
- **Persistent Settings** — Preferences saved locally in JSON and restored on launch

## Architecture

```
PosturePulse/
├── Views/
│   ├── MainWindow.xaml(.cs)       # Primary settings dashboard
│   └── ReminderPopup.xaml(.cs)    # Multi-screen reminder overlay
├── Services/
│   ├── ReminderScheduler.cs       # Timer orchestration engine
│   ├── SettingsService.cs         # JSON persistence layer
│   ├── SoundService.cs            # Embedded WAV playback
│   ├── ReminderKind.cs            # Enum: Posture | Hydration
│   └── ReminderEventArgs.cs       # Event payload
├── Models/
│   └── AppSettings.cs             # Observable settings model
├── Helpers/
│   ├── BackdropHelper.cs          # DWM interop (Mica, dark mode)
│   └── IconBuilder.cs             # Runtime multi-resolution ICO generation
├── Assets/
│   └── soft_chime.wav             # Notification sound
├── App.xaml(.cs)                  # Application entry point & tray icon
└── PosturePulse.csproj            # .NET 8 project file
```

## Build & Run

```bash
dotnet build
dotnet run
```

## Requirements

| Feature | Minimum OS |
|---|---|
| Core functionality | Windows 10 1903+ |
| Mica backdrop, rounded corners, Segoe Fluent Icons | Windows 11 22H2+ |

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

## Tech Stack

- **.NET 8** — Target framework
- **WPF** — UI framework with custom `WindowChrome` styling
- **Windows Forms** — `NotifyIcon` for system tray integration
- **DWM Interop** — Mica backdrop and immersive dark mode via P/Invoke

## License

MIT

---

*Built by [@duartelcunha](https://github.com/duartelcunha)*
