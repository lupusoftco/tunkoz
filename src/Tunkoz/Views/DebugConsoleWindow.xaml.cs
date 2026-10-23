using System.Windows;
using Tunkoz.Services;

namespace Tunkoz.Views;

public partial class DebugConsoleWindow : Window
{
    private readonly QrDetectionPipeline _pipeline;
    private bool _paused;

    public DebugConsoleWindow(QrDetectionPipeline pipeline)
    {
        InitializeComponent();
        _pipeline = pipeline;
        _pipeline.QrDetected += OnQrDetected;
    }

    private void OnQrDetected(object? sender, QrDetectionResult result)
    {
        if (_paused)
            return;

        Dispatcher.Invoke(() =>
        {
            var output = new System.Text.StringBuilder();
            output.AppendLine($"[{result.DetectionTime:HH:mm:ss.fff}] QR Detection Result");
            output.AppendLine($"Position: ({result.CursorX}, {result.CursorY})");
            output.AppendLine($"Timings: Capture={result.CaptureTimeMs}ms, Decode={result.DecodeTimeMs}ms");
            output.AppendLine($"Found: {result.Results.Count} QR code(s)");

            foreach (var qr in result.Results)
            {
                output.AppendLine($"  - Type: {qr.Payload.Type}");
                output.AppendLine($"    Content: {qr.Payload.Content}");

                if (qr.Payload.Safety != null)
                {
                    output.AppendLine($"    Safety: {qr.Payload.Safety.Severity} ({qr.Payload.Safety.ReasonKey})");
                    output.AppendLine($"    Domain: {qr.Payload.Safety.RegistrableDomain}");
                }

                if (qr.BoundingBox.HasValue)
                {
                    var box = qr.BoundingBox.Value;
                    output.AppendLine($"    Bounds: ({box.X}, {box.Y}) {box.Width}x{box.Height}");
                }
            }

            output.AppendLine();

            var current = OutputTextScroll.Text ?? "";
            OutputTextScroll.Text = output.ToString() + current;
        });
    }

    private void OnClear(object sender, RoutedEventArgs e)
    {
        OutputTextScroll.Text = "";
    }

    private void OnPause(object sender, RoutedEventArgs e)
    {
        _paused = !_paused;
        if (sender is Button btn)
        {
            btn.Content = _paused ? "Resume" : "Pause";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _pipeline.QrDetected -= OnQrDetected;
        base.OnClosed(e);
    }
}
