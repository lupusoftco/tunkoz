using System;
using System.Windows.Forms;
using Tunkoz.Views;

namespace Tunkoz.Services;

/// <summary>Owns the notification-area icon and its context menu. This is the app's only persistent UI.</summary>
public class TrayService : IDisposable
{
    private readonly QrDetectionPipeline _pipeline;
    private readonly Action _openDebugConsole;
    private readonly Action _quit;
    private readonly NotifyIcon _notifyIcon;
    private ToolStripMenuItem? _pauseItem;
    private bool _isPaused;

    public TrayService(QrDetectionPipeline pipeline, Action openDebugConsole, Action quit)
    {
        _pipeline = pipeline;
        _openDebugConsole = openDebugConsole;
        _quit = quit;

        _notifyIcon = new NotifyIcon
        {
            // Placeholder until the Claude Design icon set is wired in (Milestone 4)
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Tünköz — Active",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
        _notifyIcon.DoubleClick += (_, _) => _openDebugConsole();
    }

    public bool IsPaused => _isPaused;

    public void ShowStartedNotification()
    {
        _notifyIcon.ShowBalloonTip(
            3000,
            "Tünköz is running",
            "Right-click any QR code on screen. Tünköz lives in the system tray.",
            ToolTipIcon.Info);
    }

    public void Pause()
    {
        if (_isPaused)
            return;

        _isPaused = true;
        _pipeline.Stop();
        _notifyIcon.Text = "Tünköz — Paused";
        if (_pauseItem != null)
            _pauseItem.Text = "Resume";
    }

    public void Resume()
    {
        if (!_isPaused)
            return;

        _isPaused = false;
        _pipeline.Start();
        _notifyIcon.Text = "Tünköz — Active";
        if (_pauseItem != null)
            _pauseItem.Text = "Pause";
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        _pauseItem = new ToolStripMenuItem("Pause");
        _pauseItem.Click += (_, _) =>
        {
            if (_isPaused) Resume();
            else Pause();
        };
        menu.Items.Add(_pauseItem);

        menu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => new SettingsWindow().Show();
        menu.Items.Add(settingsItem);

        var debugItem = new ToolStripMenuItem("Debug console");
        debugItem.Click += (_, _) => _openDebugConsole();
        menu.Items.Add(debugItem);

        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (_, _) => new AboutWindow().Show();
        menu.Items.Add(aboutItem);

        menu.Items.Add(new ToolStripSeparator());

        var quitItem = new ToolStripMenuItem("Quit");
        quitItem.Click += (_, _) => _quit();
        menu.Items.Add(quitItem);

        return menu;
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
