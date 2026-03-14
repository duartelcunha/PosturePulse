using PosturePulse.Models;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace PosturePulse.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions s_writeOptions = new() { WriteIndented = true };

    private readonly string _settingsPath;
    private readonly string _tempPath;

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PosturePulse");

        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
        _tempPath = _settingsPath + ".tmp";
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new AppSettings();

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PosturePulse] Failed to load settings: {ex.Message}");
            return new AppSettings();
        }
    }

    /// <summary>
    /// Writes settings atomically: serialise → write temp → rename over target.
    /// A crash mid-write will leave the previous file intact.
    /// </summary>
    public void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, s_writeOptions);

            // Write to a sibling temp file first.
            File.WriteAllText(_tempPath, json);

            // Atomic rename; overwrites _settingsPath if it exists.
            File.Move(_tempPath, _settingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[PosturePulse] Failed to save settings: {ex.Message}");
        }
    }
}
