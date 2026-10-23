using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Tunkoz.Services;

/// <summary>
/// Follows the Windows light/dark settings live. App surfaces follow "AppsUseLightTheme";
/// the tray glyph follows "SystemUsesLightTheme" (the taskbar colour).
/// </summary>
public static class ThemeService
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private static readonly Uri LightTokens = new("pack://application:,,,/Themes/Tokens.Light.xaml");
    private static readonly Uri DarkTokens = new("pack://application:,,,/Themes/Tokens.Dark.xaml");

    private static ResourceDictionary? _current;

    public static bool AppsUseDark { get; private set; }
    public static bool TaskbarUsesDark { get; private set; }

    public static event EventHandler? ThemeChanged;

    public static void Initialize()
    {
        Apply(force: true);
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle)
                Application.Current?.Dispatcher.InvokeAsync(() => Apply(force: false));
        };
    }

    private static void Apply(bool force)
    {
        var appsDark = !ReadFlag("AppsUseLightTheme", defaultValue: true);
        var taskbarDark = !ReadFlag("SystemUsesLightTheme", defaultValue: false);

        if (!force && appsDark == AppsUseDark && taskbarDark == TaskbarUsesDark)
            return;

        AppsUseDark = appsDark;
        TaskbarUsesDark = taskbarDark;

        var merged = Application.Current.Resources.MergedDictionaries;
        var next = new ResourceDictionary { Source = appsDark ? DarkTokens : LightTokens };
        if (_current != null)
            merged.Remove(_current);
        else
        {
            // Remove the default token dictionary declared in App.xaml
            var existing = merged.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("/Themes/Tokens."));
            if (existing != null)
                merged.Remove(existing);
        }
        merged.Insert(0, next);
        _current = next;

        foreach (Window w in Application.Current.Windows)
            ApplyTitleBar(w);

        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    private static bool ReadFlag(string name, bool defaultValue)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            if (key?.GetValue(name) is int value)
                return value != 0;
        }
        catch
        {
            // ignore and use default
        }
        return defaultValue;
    }

    // --- DWM window styling -------------------------------------------------

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;
    private const int DWMWCP_DONOTROUND = 1;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    /// <summary>Dark title bar + Windows 11 rounded corners for a regular window.</summary>
    public static void ApplyTitleBar(Window window, bool round = true)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        try
        {
            var dark = AppsUseDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
            var corner = round ? DWMWCP_ROUND : DWMWCP_DONOTROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref corner, sizeof(int));
        }
        catch
        {
            // Older Windows: attributes not supported, ignore
        }
    }
}
