using System.Windows;
using Tunkoz.Services;
using Tunkoz.Views;

namespace Tunkoz;

public partial class MainWindow : Window
{
    private readonly QrDetectionPipeline _pipeline;
    private readonly TrayService _trayService;
    private DebugConsoleWindow? _debugConsole;
    private CardWindow? _currentCard;
    private bool _disposed;

    public MainWindow()
    {
        InitializeComponent();
        _pipeline = new QrDetectionPipeline();
        _trayService = new TrayService(_pipeline, this);
        _pipeline.QrDetected += OnQrDetected;
        _pipeline.Start();
    }

    private void OnQrDetected(object? sender, QrDetectionResult result)
    {
        if (result.Results.Count == 0)
            return;

        // The pipeline raises this on a background thread; WPF windows must be created on the UI thread.
        Dispatcher.InvokeAsync(() =>
        {
            if (_disposed)
                return;

            var qr = result.Results[0];

            // Only one card at a time: a new right-click replaces the previous card
            _currentCard?.Close();

            var overlay = new HighlightOverlay();
            overlay.ShowAroundQR(qr.BoundingBox);

            var card = new CardWindow();
            card.Closed += (s, e) =>
            {
                if (ReferenceEquals(_currentCard, s))
                    _currentCard = null;
            };
            _currentCard = card;
            card.DisplayResult(qr, result.CursorX, result.CursorY);
        });
    }

    private void OnDebugClick(object sender, RoutedEventArgs e)
    {
        if (_debugConsole?.IsVisible == true)
        {
            _debugConsole.Activate();
        }
        else
        {
            _debugConsole = new DebugConsoleWindow(_pipeline);
            _debugConsole.Show();
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        Cleanup();
        base.OnClosed(e);
        Application.Current.Shutdown();
    }

    private void Cleanup()
    {
        if (_disposed)
            return;
        _disposed = true;

        _currentCard?.Close();
        _debugConsole?.Close();
        _pipeline.Dispose();
        _trayService.Dispose();
    }
}
