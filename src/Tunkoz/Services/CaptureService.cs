using System.Drawing;
using System.Runtime.InteropServices;

namespace Tunkoz.Services;

/// <summary>A captured screen region. Left/Top are the region's origin in physical screen pixels.</summary>
public sealed class CapturedRegion : IDisposable
{
    public required Bitmap Bitmap { get; init; }
    public int Left { get; init; }
    public int Top { get; init; }

    public void Dispose() => Bitmap.Dispose();
}

public class CaptureService
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

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int SRCCOPY = 0x00CC0020;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int cx, int cy,
        IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    private int _captureSize = 500;

    public int CaptureSize
    {
        get => _captureSize;
        set => _captureSize = Math.Max(100, value);
    }

    /// <summary>
    /// Captures a square of <paramref name="size"/> physical px centered on the cursor,
    /// shifted/clamped so it stays inside the monitor that contains the cursor.
    /// </summary>
    public CapturedRegion? CaptureAroundCursor(int cursorX, int cursorY, int? size = null)
    {
        var captureSize = size ?? _captureSize;

        var monitor = GetMonitorBounds(cursorX, cursorY);
        var monWidth = monitor.Right - monitor.Left;
        var monHeight = monitor.Bottom - monitor.Top;

        var width = Math.Min(captureSize, monWidth);
        var height = Math.Min(captureSize, monHeight);
        if (width <= 0 || height <= 0)
            return null;

        var left = Math.Clamp(cursorX - width / 2, monitor.Left, monitor.Right - width);
        var top = Math.Clamp(cursorY - height / 2, monitor.Top, monitor.Bottom - height);

        IntPtr hScreenDC = IntPtr.Zero;
        IntPtr hMemDC = IntPtr.Zero;
        IntPtr hBitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;

        try
        {
            hScreenDC = GetDC(IntPtr.Zero);
            if (hScreenDC == IntPtr.Zero)
                return null;

            hMemDC = CreateCompatibleDC(hScreenDC);
            if (hMemDC == IntPtr.Zero)
                return null;

            hBitmap = CreateCompatibleBitmap(hScreenDC, width, height);
            if (hBitmap == IntPtr.Zero)
                return null;

            oldBitmap = SelectObject(hMemDC, hBitmap);
            var ok = BitBlt(hMemDC, 0, 0, width, height, hScreenDC, left, top, SRCCOPY);
            SelectObject(hMemDC, oldBitmap); // restore the DC's original bitmap (never delete it)

            if (!ok)
                return null;

            using var raw = Image.FromHbitmap(hBitmap);
            return new CapturedRegion
            {
                Bitmap = new Bitmap(raw),
                Left = left,
                Top = top
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
            return null;
        }
        finally
        {
            if (hBitmap != IntPtr.Zero)
                DeleteObject(hBitmap);
            if (hMemDC != IntPtr.Zero)
                DeleteDC(hMemDC);
            if (hScreenDC != IntPtr.Zero)
                ReleaseDC(IntPtr.Zero, hScreenDC);
        }
    }

    private static RECT GetMonitorBounds(int x, int y)
    {
        var hMonitor = MonitorFromPoint(new POINT { X = x, Y = y }, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        if (hMonitor != IntPtr.Zero && GetMonitorInfo(hMonitor, ref info))
            return info.rcMonitor;

        // Fallback: a generous area around the cursor
        return new RECT { Left = x - 2000, Top = y - 2000, Right = x + 2000, Bottom = y + 2000 };
    }
}
