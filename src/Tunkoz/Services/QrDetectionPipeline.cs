using Tunkoz.Core;
using Tunkoz.Core.Models;

namespace Tunkoz.Services;

public class QrDetectionResult
{
    public int CursorX { get; set; }
    public int CursorY { get; set; }
    public long CaptureTimeMs { get; set; }
    public long DecodeTimeMs { get; set; }
    public List<QrResult> Results { get; set; } = new();
    public DateTime DetectionTime { get; set; }
}

public class QrDetectionPipeline : IDisposable
{
    private readonly MouseHookService _mouseHook;
    private readonly CaptureService _captureService;
    private readonly QrDecoderService _decoderService;

    public event EventHandler<QrDetectionResult>? QrDetected;

    public QrDetectionPipeline()
    {
        _mouseHook = new MouseHookService();
        _captureService = new CaptureService();
        _decoderService = new QrDecoderService();

        _mouseHook.RightClickDetected += OnRightClickDetected;
    }

    public void Start()
    {
        _mouseHook.Start();
    }

    public void Stop()
    {
        _mouseHook.Stop();
    }

    public int CaptureSize
    {
        get => _captureService.CaptureSize;
        set => _captureService.CaptureSize = value;
    }

    private void OnRightClickDetected(object? sender, MouseClickEventArgs e)
    {
        _ = ProcessClickAsync(e);
    }

    private async Task ProcessClickAsync(MouseClickEventArgs e)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = new QrDetectionResult
        {
            CursorX = e.X,
            CursorY = e.Y,
            DetectionTime = DateTime.UtcNow
        };

        try
        {
            // Capture phase
            var captureStopwatch = System.Diagnostics.Stopwatch.StartNew();
            using var bitmap = _captureService.CaptureAroundCursorWithRetry(e.X, e.Y);
            captureStopwatch.Stop();
            result.CaptureTimeMs = captureStopwatch.ElapsedMilliseconds;

            if (bitmap == null)
            {
                System.Diagnostics.Debug.WriteLine("Capture failed");
                return;
            }

            // Decode phase
            var decodeStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var qrResults = _decoderService.DecodeFromBitmap(bitmap, e.X, e.Y);
            decodeStopwatch.Stop();
            result.DecodeTimeMs = decodeStopwatch.ElapsedMilliseconds;

            result.Results = qrResults;

            stopwatch.Stop();
            System.Diagnostics.Debug.WriteLine(
                $"[QR Detection] Total: {stopwatch.ElapsedMilliseconds} ms | " +
                $"Capture: {result.CaptureTimeMs} ms | " +
                $"Decode: {result.DecodeTimeMs} ms | " +
                $"Found: {qrResults.Count} QR(s)");

            if (qrResults.Count > 0)
            {
                QrDetected?.Invoke(this, result);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Detection pipeline error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _mouseHook?.Dispose();
    }
}
