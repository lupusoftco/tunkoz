using System.Drawing;
using System.Runtime.InteropServices;

namespace Tunkoz.Services;

public class CaptureService
{
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

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

    private const int SRCCOPY = 0x00CC0020;

    private int _captureSize = 500;

    public int CaptureSize
    {
        get => _captureSize;
        set => _captureSize = Math.Max(100, value);
    }

    public Bitmap? CaptureAroundCursor(int cursorX, int cursorY)
    {
        var halfSize = _captureSize / 2;
        var left = cursorX - halfSize;
        var top = cursorY - halfSize;

        // Clamp to virtual screen bounds
        var screenX = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var screenY = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var screenWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var screenHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        left = Math.Max(left, screenX);
        top = Math.Max(top, screenY);

        var right = Math.Min(left + _captureSize, screenX + screenWidth);
        var bottom = Math.Min(top + _captureSize, screenY + screenHeight);

        var width = right - left;
        var height = bottom - top;

        if (width <= 0 || height <= 0)
            return null;

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

            if (!BitBlt(hMemDC, 0, 0, width, height, hScreenDC, left, top, SRCCOPY))
                return null;

            SelectObject(hMemDC, oldBitmap);

            var bitmap = Image.FromHbitmap(hBitmap);
            return new Bitmap(bitmap);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Capture error: {ex.Message}");
            return null;
        }
        finally
        {
            if (oldBitmap != IntPtr.Zero)
                DeleteObject(oldBitmap);
            if (hBitmap != IntPtr.Zero)
                DeleteObject(hBitmap);
            if (hMemDC != IntPtr.Zero)
                DeleteDC(hMemDC);
            if (hScreenDC != IntPtr.Zero)
                ReleaseDC(IntPtr.Zero, hScreenDC);
        }
    }

    public Bitmap? CaptureAroundCursorWithRetry(int cursorX, int cursorY)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // First attempt
        var bitmap = CaptureAroundCursor(cursorX, cursorY);
        if (bitmap != null)
        {
            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine($"Capture completed in {stopwatch.ElapsedMilliseconds} ms");
            return bitmap;
        }

        // Second attempt at 2x size
        var originalSize = _captureSize;
        _captureSize *= 2;

        bitmap = CaptureAroundCursor(cursorX, cursorY);
        _captureSize = originalSize;

        stopwatch.Stop();
        System.Diagnostics.Debug.WriteLine($"Capture (retry) completed in {stopwatch.ElapsedMilliseconds} ms");
        return bitmap;
    }
}
