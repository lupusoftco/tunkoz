using System.Windows;
using Tunkoz.Services;
using Tunkoz.Views;

namespace Tunkoz;

public partial class MainWindow : Window
{
    private readonly QrDetectionPipeline _pipeline;
    private readonly TrayService _trayService;
    private DebugConsoleWindow? _debugConsole;

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

        var firstResult = result.Results[0];

        // Show highlight overlay around detected QR
        var overlay = new HighlightOverlay();
        overlay.ShowAroundQR(firstResult.BoundingBox);

        // Show result card
        var cardWindow = new CardWindow();
        cardWindow.DisplayResult(firstResult, result.CursorX, result.CursorY);
    }

    private void OnDebugClick(object sender, RoutedEventArgs e)
    {
        if (_debugConsole?.IsVisible == true)
        {
            _debugConsole.Focus();
        }
        else
        {
            _debugConsole = new DebugConsoleWindow(_pipeline);
            _debugConsole.Show();
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        _debugConsole?.Close();
        _pipeline.Dispose();
        _trayService.Dispose();
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _pipeline.Dispose();
        _trayService.Dispose();
        base.OnClosed(e);
    }
}
