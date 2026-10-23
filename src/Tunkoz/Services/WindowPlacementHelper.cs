using System.Runtime.InteropServices;
using System.Windows;
using CoreRect = Tunkoz.Core.Models.Rectangle;

namespace Tunkoz.Services;

/// <summary>
/// Positions popup windows. All inputs are physical screen pixels (from the hook / capture);
/// WPF Left/Top/Width/Height are DIPs, so results are converted with the current DPI scale.
/// </summary>
public static class WindowPlacementHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const double Gap = 8;
    private const double DefaultWidthDip = 336;
    private const double DefaultHeightDip = 180;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    /// <summary>Physical-pixels-per-DIP for the current process (system DPI; the app is system-DPI-aware).</summary>
    public static double GetDpiScale()
    {
        try
        {
            var dpi = GetDpiForSystem();
            if (dpi > 0)
                return dpi / 96.0;
        }
        catch (EntryPointNotFoundException)
        {
            // Pre-1607 Windows 10: fall back to WPF's view of the system DPI
        }

        return SystemParameters.PrimaryScreenHeight > 0
            ? System.Windows.Forms.Screen.PrimaryScreen!.Bounds.Height / SystemParameters.PrimaryScreenHeight
            : 1.0;
    }

    /// <param name="shadowMarginDip">Transparent margin around the visible card (for its shadow);
    /// placement uses the visible card's edges.</param>
    public static void PositionWindowSmartly(Window window, int cursorX, int cursorY, CoreRect? qrBounds, double shadowMarginDip = 0)
    {
        var scale = GetDpiScale();
        var marginPx = shadowMarginDip * scale;
        var widthPx = ((window.ActualWidth > 0 ? window.ActualWidth : DefaultWidthDip) - 2 * shadowMarginDip) * scale;
        var heightPx = ((window.ActualHeight > 0 ? window.ActualHeight : DefaultHeightDip) - 2 * shadowMarginDip) * scale;
        var work = GetWorkArea(cursorX, cursorY);

        double left, top;

        var menu = FindContextMenu();
        if (menu.HasValue)
        {
            var m = menu.Value;
            // Prefer the right side of the native menu, otherwise the left side
            left = m.Right + Gap * scale;
            if (left + widthPx > work.Right)
                left = m.Left - Gap * scale - widthPx;
            top = m.Top;
        }
        else
        {
            var box = qrBounds ?? new CoreRect { X = cursorX - 50, Y = cursorY - 50, Width = 100, Height = 100 };

            // Below the QR first, then above, then to the right
            left = box.X + (box.Width - widthPx) / 2;
            top = box.Y + box.Height + Gap * scale;

            if (top + heightPx > work.Bottom)
                top = box.Y - heightPx - Gap * scale;

            if (top < work.Top)
            {
                left = box.X + box.Width + Gap * scale;
                top = box.Y;
            }
        }

        // Clamp to the work area
        left = Math.Max(work.Left, Math.Min(left, work.Right - widthPx));
        top = Math.Max(work.Top, Math.Min(top, work.Bottom - heightPx));

        // Never cover the cursor (native menus open at the cursor)
        if (cursorX >= left && cursorX <= left + widthPx && cursorY >= top && cursorY <= top + heightPx)
        {
            var below = cursorY + 24 * scale;
            top = below + heightPx <= work.Bottom ? below : cursorY - heightPx - 24 * scale;
            top = Math.Max(work.Top, top);
        }

        window.Left = (left - marginPx) / scale;
        window.Top = (top - marginPx) / scale;
    }

    /// <summary>Sets a window to exactly cover a physical-pixel rectangle.</summary>
    public static void PlaceOverRect(Window window, CoreRect rect, int paddingPx)
    {
        var scale = GetDpiScale();
        window.Left = (rect.X - paddingPx) / scale;
        window.Top = (rect.Y - paddingPx) / scale;
        window.Width = (rect.Width + paddingPx * 2) / scale;
        window.Height = (rect.Height + paddingPx * 2) / scale;
    }

    private static RECT? FindContextMenu()
    {
        // Classic Win32 popup menus use the "#32768" window class
        var hwnd = FindWindow("#32768", null);
        if (hwnd != IntPtr.Zero && IsWindowVisible(hwnd) && GetWindowRect(hwnd, out var rect))
            return rect;
        return null;
    }

    /// <summary>Work area (physical px) of the monitor containing the point.</summary>
    public static System.Drawing.Rectangle GetWorkAreaPx(int x, int y)
    {
        var r = GetWorkArea(x, y);
        return System.Drawing.Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
    }

    private static RECT GetWorkArea(int x, int y)
    {
        var hMonitor = MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (hMonitor != IntPtr.Zero && GetMonitorInfo(hMonitor, ref info))
            return info.rcWork;

        var sw = SystemParameters.WorkArea;
        var s = GetDpiScale();
        return new RECT
        {
            Left = (int)(sw.Left * s),
            Top = (int)(sw.Top * s),
            Right = (int)(sw.Right * s),
            Bottom = (int)(sw.Bottom * s)
        };
    }
}
