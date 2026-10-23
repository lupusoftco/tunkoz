using System.Windows;
using System.Windows.Forms;
using Tunkoz.Views;
using Application = System.Windows.Application;

namespace Tunkoz.Services;

/// <summary>
/// Owns the notification-area icon (design: monochrome eye, white on a dark taskbar, #1B1B1B on a light one)
/// and opens the custom Windows 11-style tray menu on click.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly Action _openSettings;
    private readonly Action _openAbout;
    private readonly Action _quit;
    private readonly NotifyIcon _notifyIcon;
    private TrayMenuWindow? _menu;
    private bool _isPaused;

    public TrayService(Action openSettings, Action openAbout, Action quit)
    {
        _openSettings = openSettings;
        _openAbout = openAbout;
        _quit = quit;

        _notifyIcon = new NotifyIcon { Visible = false };
        _notifyIcon.MouseUp += (_, e) =>
        {
            if (e.Button is MouseButtons.Right or MouseButtons.Left)
                ShowMenu();
        };

        UpdateIcon();
        _notifyIcon.Visible = true;

        ThemeService.ThemeChanged += (_, _) => UpdateIcon();
        Loc.Instance.LanguageChanged += (_, _) => UpdateIcon();
    }

    public bool IsPaused
    {
        get => _isPaused;
        set { _isPaused = value; UpdateIcon(); }
    }

    public void ShowStartedNotification()
    {
        _notifyIcon.ShowBalloonTip(3000, Loc.T("Tray.Started.Title"), Loc.T("Tray.Started.Body"), ToolTipIcon.None);
    }

    private void ShowMenu()
    {
        if (_menu != null)
        {
            _menu.Close();
            return;
        }

        var pos = Cursor.Position; // physical px
        _menu = new TrayMenuWindow(_openSettings, _openAbout, _quit);
        _menu.Closed += (_, _) => _menu = null;
        _menu.ShowAt(pos.X, pos.Y);
    }

    private void UpdateIcon()
    {
        var state = _isPaused ? "paused" : "active";
        var variant = ThemeService.TaskbarUsesDark ? "dark" : "light";
        var old = _notifyIcon.Icon;
        _notifyIcon.Icon = LoadIcon($"tray-{state}-{variant}.ico");
        old?.Dispose();
        _notifyIcon.Text = Loc.T(_isPaused ? "Tray.Tooltip.Paused" : "Tray.Tooltip.Active");
    }

    private static System.Drawing.Icon LoadIcon(string name)
    {
        var info = Application.GetResourceStream(new Uri($"pack://application:,,,/Assets/{name}"));
        if (info == null)
            return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
        using var stream = info.Stream;
        return new System.Drawing.Icon(stream, SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        _menu?.Close();
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.Dispose();
    }
}
