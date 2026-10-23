using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Tunkoz.Services;

public class WindowPlacementHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out System.Drawing.Rectangle lpRect);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(System.Drawing.Point pt, uint dwFlags);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public System.Drawing.Rectangle rcMonitor;
        public System.Drawing.Rectangle rcWork;
        public uint dwFlags;
    }

    public static void PositionWindowSmartly(
        Window window,
        int cursorX,
        int cursorY,
        System.Drawing.Rectangle qrBounds)
    {
        // Try to find native context menu (class "#32768")
        var contextMenuHandle = FindContextMenu();

        if (contextMenuHandle != IntPtr.Zero && GetWindowRect(contextMenuHandle, out var menuRect))
        {
            // Position beside the context menu
            PositionBesideContextMenu(window, menuRect, cursorX, cursorY);
        }
        else
        {
            // Position beside QR bounds
            PositionBesideQR(window, qrBounds, cursorX, cursorY);
        }
    }

    private static IntPtr FindContextMenu()
    {
        // Search for the standard Windows context menu window
        // This is a heuristic; real implementation might need to search more thoroughly
        return FindWindowEx(IntPtr.Zero, IntPtr.Zero, "#32768", null);
    }

    private static void PositionBesideContextMenu(
        Window window,
        System.Drawing.Rectangle menuRect,
        int cursorX,
        int cursorY)
    {
        var monitor = GetMonitorFromCursor(cursorX, cursorY);

        // Try to position to the right of the menu
        double proposedLeft = menuRect.Right + 10;
        double proposedTop = menuRect.Top;

        ClampToMonitor(window, ref proposedLeft, ref proposedTop, monitor);

        window.Left = proposedLeft;
        window.Top = proposedTop;
    }

    private static void PositionBesideQR(
        Window window,
        System.Drawing.Rectangle qrBounds,
        int cursorX,
        int cursorY)
    {
        var monitor = GetMonitorFromCursor(cursorX, cursorY);

        double windowWidth = window.ActualWidth > 0 ? window.ActualWidth : 336;
        double windowHeight = window.ActualHeight > 0 ? window.ActualHeight : 200;

        // Try positioning above QR
        double proposedLeft = qrBounds.Left + (qrBounds.Width - windowWidth) / 2;
        double proposedTop = qrBounds.Top - windowHeight - 10;

        // Check if it fits; if not, try below
        if (!FitsInMonitor(proposedLeft, proposedTop, windowWidth, windowHeight, monitor))
        {
            proposedTop = qrBounds.Bottom + 10;
        }

        // Check again; if still doesn't fit, try to the right
        if (!FitsInMonitor(proposedLeft, proposedTop, windowWidth, windowHeight, monitor))
        {
            proposedLeft = qrBounds.Right + 10;
            proposedTop = qrBounds.Top + (qrBounds.Height - windowHeight) / 2;
        }

        // Final fallback: clamp to monitor
        ClampToMonitor(window, ref proposedLeft, ref proposedTop, monitor);

        // Ensure cursor is not covered
        if (IsPointInWindow(cursorX, cursorY, proposedLeft, proposedTop, windowWidth, windowHeight))
        {
            proposedTop = Math.Max(proposedTop, cursorY + 20);
        }

        window.Left = proposedLeft;
        window.Top = proposedTop;
    }

    private static MONITORINFO GetMonitorFromCursor(int cursorX, int cursorY)
    {
        var point = new System.Drawing.Point(cursorX, cursorY);
        var hMonitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);

        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
        GetMonitorInfo(hMonitor, ref info);
        return info;
    }

    private static bool FitsInMonitor(
        double left,
        double top,
        double width,
        double height,
        MONITORINFO monitor)
    {
        var work = monitor.rcWork;
        return left >= work.Left &&
               top >= work.Top &&
               left + width <= work.Right &&
               top + height <= work.Bottom;
    }

    private static void ClampToMonitor(
        Window window,
        ref double left,
        ref double top,
        MONITORINFO monitor)
    {
        var work = monitor.rcWork;
        double windowWidth = window.ActualWidth > 0 ? window.ActualWidth : 336;
        double windowHeight = window.ActualHeight > 0 ? window.ActualHeight : 200;

        left = Math.Max(work.Left, Math.Min(left, work.Right - windowWidth));
        top = Math.Max(work.Top, Math.Min(top, work.Bottom - windowHeight));
    }

    private static bool IsPointInWindow(
        int pointX,
        int pointY,
        double windowLeft,
        double windowTop,
        double windowWidth,
        double windowHeight)
    {
        return pointX >= windowLeft &&
               pointX <= windowLeft + windowWidth &&
               pointY >= windowTop &&
               pointY <= windowTop + windowHeight;
    }
}
