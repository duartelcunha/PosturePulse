using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace PosturePulse.Helpers;

/// <summary>
/// Enables Windows 11 Mica / Mica-Alt / Acrylic backdrop on a WPF window.
/// Falls back gracefully on older Windows versions.
/// </summary>
public static class BackdropHelper
{
    // DWM attribute constants
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWMWA_MICA_EFFECT = 1029;

    // Backdrop types for DWMWA_SYSTEMBACKDROP_TYPE (Win11 22H2+)
    private const int DWMSBT_MAINWINDOW = 2;   // Mica
    private const int DWMSBT_TABBEDWINDOW = 4; // Mica-Alt

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

    [StructLayout(LayoutKind.Sequential)]
    private struct MARGINS
    {
        public int Left, Right, Top, Bottom;
    }

    /// <summary>
    /// Applies Mica backdrop to the given window. Call after the window handle is available.
    /// The window must have a transparent or very-low-alpha background for the effect to show.
    /// </summary>
    public static bool ApplyMica(Window window, bool useMicaAlt = false)
    {
        if (window is null) return false;

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return false;

        // Enable dark mode for the caption area
        int darkMode = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        // Extend frame into entire client area (needed for backdrop)
        var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);

        // Try Win11 22H2+ system backdrop API first
        int backdropType = useMicaAlt ? DWMSBT_TABBEDWINDOW : DWMSBT_MAINWINDOW;
        int result = DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

        if (result != 0)
        {
            // Fallback for Win11 21H2: use the older undocumented attribute
            int micaEnabled = 1;
            result = DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref micaEnabled, sizeof(int));
        }

        return result == 0;
    }

    /// <summary>
    /// Applies dark mode to the DWM title bar (useful even with WindowChrome).
    /// </summary>
    public static void ApplyDarkTitleBar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        int darkMode = 1;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
    }
}
