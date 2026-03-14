using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace PosturePulse.Services;

public class SoundService
{
    private readonly MediaPlayer _player = new();
    private bool _loaded;
    private string? _cachedSoundPath;

    public void Play(double volumePercent)
    {
        try
        {
            if (!_loaded)
            {
                _cachedSoundPath = EnsureSoundExtracted();
                if (_cachedSoundPath is null)
                    return;

                _player.Open(new Uri(_cachedSoundPath, UriKind.Absolute));
                _loaded = true;
            }

            _player.Volume = Math.Clamp(volumePercent / 100d, 0d, 1d);
            _player.Position = TimeSpan.Zero;
            _player.Play();
        }
        catch
        {
            // Silent failure keeps the reminder flow uninterrupted.
        }
    }

    /// <summary>
    /// Extracts the embedded WAV to %TEMP%\PosturePulse only if it is not
    /// already present, avoiding redundant disk writes on every launch.
    /// </summary>
    private static string? EnsureSoundExtracted()
    {
        const string resourceName = "PosturePulse.Assets.soft_chime.wav";

        var folder = Path.Combine(Path.GetTempPath(), "PosturePulse");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "soft_chime.wav");

        // Skip extraction if the file is already there.
        if (File.Exists(path))
            return path;

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return null;

        using var file = File.Create(path);
        stream.CopyTo(file);
        return path;
    }
}
