using System;
using System.Windows;
using System.Windows.Forms;
using Tunkoz.Views;

namespace Tunkoz.Services;

public class TrayService : IDisposable
{
    private readonly QrDetectionPipeline _pipeline;
    private readonly NotifyIcon _notifyIcon;
    private bool _isPaused;

    public TrayService(QrDetectionPipeline pipeline, Window mainWindow)
    {
        _pipeline = pipeline;
        _notifyIcon = new NotifyIcon();

        // Set up tray icon (use a placeholder emoji/text for now)
        _notifyIcon.Icon = SystemIcons.Application;
        _notifyIcon.Text = "Tünköz - QR Detector";
        _notifyIcon.Visible = true;
        _notifyIcon.ContextMenuStrip = BuildContextMenu(mainWindow);
    }

    public void Pause()
    {
        if (_isPaused)
            return;

        _isPaused = true;
        _pipeline.Stop();
        _notifyIcon.Text = "Tünköz - Paused";
    }

    public void Resume()
    {
        if (!_isPaused)
            return;

        _isPaused = false;
        _pipeline.Start();
        _notifyIcon.Text = "Tünköz - Active";
    }

    public bool IsPaused => _isPaused;

    private ContextMenuStrip BuildContextMenu(Window mainWindow)
    {
        var menu = new ContextMenuStrip();

        // Pause/Resume
        var pauseItem = new ToolStripMenuItem(_isPaused ? "Resume" : "Pause");
        pauseItem.Click += (s, e) =>
        {
            if (_isPaused)
                Resume();
            else
                Pause();

            pauseItem.Text = _isPaused ? "Resume" : "Pause";
        };
        menu.Items.Add(pauseItem);

        // Separator
        menu.Items.Add(new ToolStripSeparator());

        // Settings
        var settingsItem = new ToolStripMenuItem("Settings");
        settingsItem.Click += (s, e) =>
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        };
        menu.Items.Add(settingsItem);

        // About
        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += (s, e) =>
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        };
        menu.Items.Add(aboutItem);

        // Separator
        menu.Items.Add(new ToolStripSeparator());

        // Start with Windows (placeholder)
        var startupItem = new ToolStripMenuItem("Start with Windows", null, (s, e) =>
        {
            // TODO: Implement registry autostart logic
        });
        menu.Items.Add(startupItem);

        // Separator
        menu.Items.Add(new ToolStripSeparator());

        // Quit
        var quitItem = new ToolStripMenuItem("Quit");
        quitItem.Click += (s, e) =>
        {
            Application.Current.Shutdown();
        };
        menu.Items.Add(quitItem);

        return menu;
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}

// Simple relay command for MVVM compatibility
public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object> _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler CanExecuteChanged
    {
        add { System.Windows.Input.CommandManager.RequerySuggested += value; }
        remove { System.Windows.Input.CommandManager.RequerySuggested -= value; }
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object parameter) => _execute(parameter);
}
