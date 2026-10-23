using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Tunkoz.Services;
using CoreRect = Tunkoz.Core.Models.Rectangle;

namespace Tunkoz.Views;

/// <summary>Click-through amber finder-corner brackets around the detected QR, then a fade-out.</summary>
public partial class HighlightOverlay : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public HighlightOverlay()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        };
    }

    /// <summary>Outlines a QR given in physical screen pixels, holds briefly, then fades (~300 ms).</summary>
    public void ShowAroundQR(CoreRect? bounds)
    {
        if (bounds is not { } rect || rect.Width <= 0 || rect.Height <= 0)
        {
            Close();
            return;
        }

        // ZXing reports finder-pattern centres, which sit inside the code; pad out to the code's edge
        WindowPlacementHelper.PlaceOverRect(this, rect, paddingPx: 14);
        Brackets.Data = BuildBrackets(Width, Height);
        Show();

        var fadeOut = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            BeginTime = TimeSpan.FromMilliseconds(450),
            Duration = TimeSpan.FromMilliseconds(300)
        };
        fadeOut.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private static Geometry BuildBrackets(double w, double h)
    {
        // Corner length scales with the box (28 px on a 224 px box in the mockup)
        var l = Math.Max(10, Math.Min(28, Math.Min(w, h) / 8));
        const double i = 2;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            void Corner(Point a, Point b, Point c)
            {
                ctx.BeginFigure(a, isFilled: false, isClosed: false);
                ctx.LineTo(b, isStroked: true, isSmoothJoin: false);
                ctx.LineTo(c, isStroked: true, isSmoothJoin: false);
            }

            Corner(new Point(i, l), new Point(i, i), new Point(l, i));
            Corner(new Point(w - l, i), new Point(w - i, i), new Point(w - i, l));
            Corner(new Point(w - i, h - l), new Point(w - i, h - i), new Point(w - l, h - i));
            Corner(new Point(l, h - i), new Point(i, h - i), new Point(i, h - l));
        }
        geometry.Freeze();
        return geometry;
    }
}
