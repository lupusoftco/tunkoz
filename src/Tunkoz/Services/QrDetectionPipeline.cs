using Tunkoz.Core;
using Tunkoz.Core.Models;

namespace Tunkoz.Services;

public class QrDetectionResult
{
    public int CursorX { get; set; }
    public int CursorY { get; set; }
    public long CaptureTimeMs { get; set; }
    public long DecodeTimeMs { get; set; }
    public long TotalTimeMs { get; set; }
    public bool UsedSecondPass { get; set; }

    /// <summary>Decoded QR codes. Bounding boxes are in physical screen pixels.</summary>
    public List<QrResult> Results { get; set; } = new();
    public DateTime DetectionTime { get; set; }
}

public class QrDetectionPipeline : IDisposable
{
    private readonly MouseHookService _mouseHook;
    private readonly CaptureService _captureService;
    private readonly QrDecoderService _decoderService;
    private readonly object _decodeLock = new();

    /// <summary>Raised on a thread-pool thread. Marshal to the UI thread before touching WPF.</summary>
    public event EventHandler<QrDetectionResult>? QrDetected;

    public QrDetectionPipeline()
    {
        _mouseHook = new MouseHookService();
        _captureService = new CaptureService();
        _decoderService = new QrDecoderService();

        _mouseHook.RightClickDetected += OnRightClickDetected;
    }

    public void Start() => _mouseHook.Start();

    public void Stop() => _mouseHook.Stop();

    public int CaptureSize
    {
        get => _captureService.CaptureSize;
        set => _captureService.CaptureSize = value;
    }

    private void OnRightClickDetected(object? sender, MouseClickEventArgs e)
    {
        _ = Task.Run(() => ProcessClick(e));
    }

    private void ProcessClick(MouseClickEventArgs e)
    {
        var total = System.Diagnostics.Stopwatch.StartNew();
        var result = new QrDetectionResult
        {
            CursorX = e.X,
            CursorY = e.Y,
            DetectionTime = DateTime.Now
        };

        try
        {
            var size = _captureService.CaptureSize;
            var found = CaptureAndDecode(e, size, result);

            // Second pass at 2x size if nothing (or only a partial QR) was found
            if (found.Count == 0)
            {
                result.UsedSecondPass = true;
                found = CaptureAndDecode(e, size * 2, result);
            }

            result.Results = found;
            total.Stop();
            result.TotalTimeMs = total.ElapsedMilliseconds;

            System.Diagnostics.Debug.WriteLine(
                $"[QR Detection] Total: {result.TotalTimeMs} ms | Capture: {result.CaptureTimeMs} ms | " +
                $"Decode: {result.DecodeTimeMs} ms | 2nd pass: {result.UsedSecondPass} | Found: {found.Count}");

            if (found.Count > 0)
                QrDetected?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Detection pipeline error: {ex}");
        }
    }

    private List<QrResult> CaptureAndDecode(MouseClickEventArgs e, int size, QrDetectionResult timings)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        using var region = _captureService.CaptureAroundCursor(e.X, e.Y, size);
        timings.CaptureTimeMs += sw.ElapsedMilliseconds;

        if (region == null)
            return new List<QrResult>();

        sw.Restart();
        List<QrResult> found;
        lock (_decodeLock) // ZXing reader instance is not thread-safe
        {
            found = _decoderService.DecodeFromBitmap(region.Bitmap, e.X - region.Left, e.Y - region.Top);
        }
        timings.DecodeTimeMs += sw.ElapsedMilliseconds;

        // Convert bounding boxes from bitmap coordinates to screen coordinates
        foreach (var qr in found)
        {
            if (qr.BoundingBox is { } box)
            {
                qr.BoundingBox = new Rectangle
                {
                    X = box.X + region.Left,
                    Y = box.Y + region.Top,
                    Width = box.Width,
                    Height = box.Height
                };
            }
        }

        return found;
    }

    public void Dispose()
    {
        _mouseHook.Dispose();
    }
}
