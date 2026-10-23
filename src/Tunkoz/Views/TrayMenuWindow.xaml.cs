using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Tunkoz.Services;

namespace Tunkoz.Views;

/// <summary>Windows 11-style tray menu from the Claude Design artifact. Closes when it loses focus.</summary>
public partial class TrayMenuWindow : Window
{
    private const double ShadowMargin = 16;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private readonly Action _openSettings;
    private readonly Action _openAbout;
    private readonly Action _quit;
    private bool _closing;

    public TrayMenuWindow(Action openSettings, Action openAbout, Action quit)
    {
        InitializeComponent();
        _openSettings = openSettings;
        _openAbout = openAbout;
        _quit = quit;
        Deactivated += (_, _) => SafeClose();
    }

    /// <summary>Opens with its bottom-right corner at the cursor (physical px), kept inside the work area.</summary>
    public void ShowAt(int cursorX, int cursorY)
    {
        Left = -32000;
        Top = -32000;
        Show();
        UpdateLayout();

        var scale = WindowPlacementHelper.GetDpiScale();
        var work = WindowPlacementHelper.GetWorkAreaPx(cursorX, cursorY);
        var widthPx = (ActualWidth - 2 * ShadowMargin) * scale;
        var heightPx = (ActualHeight - 2 * ShadowMargin) * scale;

        var left = cursorX - widthPx;
        var top = cursorY - heightPx;
        if (left < work.Left) left = cursorX;
        if (top < work.Top) top = cursorY;
        left = Math.Max(work.Left, Math.Min(left, work.Right - widthPx));
        top = Math.Max(work.Top, Math.Min(top, work.Bottom - heightPx));

        Left = left / scale - ShadowMargin;
        Top = top / scale - ShadowMargin;

        Activate();
        SetForegroundWindow(new WindowInteropHelper(this).Handle);
        SettingsItem.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SafeClose();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    private void SafeClose()
    {
        if (_closing) return;
        _closing = true;
        Close();
    }

    private void OnSettings(object sender, RoutedEventArgs e) { SafeClose(); _openSettings(); }
    private void OnAbout(object sender, RoutedEventArgs e) { SafeClose(); _openAbout(); }
    private void OnQuit(object sender, RoutedEventArgs e) { SafeClose(); _quit(); }
}
