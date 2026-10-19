using System.Runtime.InteropServices;

namespace WinSymlinkFinder.Helpers;

public static class DpiHelper
{
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    /// <summary>
    /// Récupère le DPI associé à la fenêtre WinUI / Win32.
    /// Retourne 96 (100%) par défaut en cas d'impossibilité de détection.
    /// </summary>
    public static uint GetWindowDpi(IntPtr hWnd)
    {
        if (hWnd != IntPtr.Zero)
        {
            uint dpi = GetDpiForWindow(hWnd);
            if (dpi > 0)
            {
                return dpi;
            }

            IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);
            if (hMonitor != IntPtr.Zero && GetDpiForMonitor(hMonitor, 0 /* MDT_EFFECTIVE_DPI */, out uint dpiX, out _) == 0 && dpiX > 0)
            {
                return dpiX;
            }
        }

        return 96;
    }

    /// <summary>
    /// Calcule le facteur d'échelle (ex: 1.0 pour 100%, 1.25 pour 125%, 1.5 pour 150%, 2.0 pour 200%).
    /// </summary>
    public static double GetScaleFactor(IntPtr hWnd)
    {
        return GetWindowDpi(hWnd) / 96.0;
    }
}
