using System.Threading;
using System.Windows;
using Tunkoz.Services;
using Tunkoz.Views;

namespace Tunkoz;

/// <summary>
/// Tray-only application: no main window. The tray icon is the only persistent UI;
/// the result card appears when a QR code is right-clicked.
/// Run with --debug to also open the detection console (timings, decoded payloads).
/// </summary>
public partial class App : Application
{
    private const string SingleInstanceMutexName = "Tunkoz.SingleInstance.7F3A2C1E";

    private Mutex? _singleInstanceMutex;
    private QrDetectionPipeline? _pipeline;
    private TrayService? _trayService;
    private DebugConsoleWindow? _debugConsole;
    private SettingsWindow? _settings;
    private AboutWindow? _about;
    private CardWindow? _currentCard;
    private bool _shuttingDown;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // A second instance would install a second hook and show duplicate cards
        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        SettingsStore.Load();
        Loc.Instance.Language = SettingsStore.EffectiveLanguage;
        ThemeService.Initialize();

        _pipeline = new QrDetectionPipeline();
        _pipeline.QrDetected += OnQrDetected;

        _trayService = new TrayService(OpenSettings, OpenAbout, () => Shutdown());
        _pipeline.Start();
        _trayService.ShowStartedNotification();

        if (e.Args.Contains("--debug", StringComparer.OrdinalIgnoreCase))
            OpenDebugConsole();
    }

    private void OnQrDetected(object? sender, QrDetectionResult result)
    {
        if (result.Results.Count == 0)
            return;

        // The pipeline raises this on a background thread; WPF windows must be created on the UI thread.
        Dispatcher.InvokeAsync(() =>
        {
            if (_shuttingDown)
                return;

            var qr = result.Results[0];

            // Only one card at a time: a new right-click replaces the previous card
            _currentCard?.Close();

            var overlay = new HighlightOverlay();
            overlay.ShowAroundQR(qr.BoundingBox);

            var card = new CardWindow();
            card.Closed += (s, _) =>
            {
                if (ReferenceEquals(_currentCard, s))
                    _currentCard = null;
            };
            _currentCard = card;
            card.DisplayResult(qr, result.CursorX, result.CursorY);
        });
    }

    private void OpenSettings()
    {
        if (_settings != null)
        {
            _settings.Activate();
            return;
        }
        _settings = new SettingsWindow();
        _settings.Closed += (_, _) => _settings = null;
        _settings.Show();
        _settings.Activate();
    }

    private void OpenAbout()
    {
        if (_about != null)
        {
            _about.Activate();
            return;
        }
        _about = new AboutWindow();
        _about.Closed += (_, _) => _about = null;
        _about.Show();
        _about.Activate();
    }

    private void OpenDebugConsole()
    {
        if (_pipeline == null)
            return;

        if (_debugConsole != null)
        {
            _debugConsole.Activate();
            return;
        }

        _debugConsole = new DebugConsoleWindow(_pipeline);
        _debugConsole.Closed += (_, _) => _debugConsole = null;
        _debugConsole.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _shuttingDown = true;
        _currentCard?.Close();
        _debugConsole?.Close();
        _settings?.Close();
        _about?.Close();
        _pipeline?.Dispose();
        _trayService?.Dispose();

        if (_singleInstanceMutex != null)
        {
            _singleInstanceMutex.ReleaseMutex();
            _singleInstanceMutex.Dispose();
        }

        base.OnExit(e);
    }
}
