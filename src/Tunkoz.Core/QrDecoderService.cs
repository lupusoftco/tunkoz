using System.Drawing;
using ZXing;
using ZXing.Common;
using Tunkoz.Core.Models;

namespace Tunkoz.Core;

public class QrDecoderService
{
    private readonly IReader _reader;
    private readonly PayloadParser _parser;
    private readonly LinkSafetyAnalyzer _safety;

    public QrDecoderService(PayloadParser? parser = null, LinkSafetyAnalyzer? safety = null)
    {
        var hints = new Dictionary<DecodeHintType, object>
        {
            { DecodeHintType.TRY_HARDER, true },
            { DecodeHintType.ALSO_INVERTED, true },
            { DecodeHintType.POSSIBLE_FORMATS, new[] { BarcodeFormat.QR_CODE } }
        };

        _reader = new MultiFormatReader { Hints = hints };
        _parser = parser ?? new PayloadParser();
        _safety = safety ?? new LinkSafetyAnalyzer();
    }

    public List<QrResult> DecodeFromBitmap(Bitmap bitmap, int? cursorX = null, int? cursorY = null)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var results = new List<QrResult>();

        try
        {
            var source = new BitmapLuminanceSource(bitmap);
            var binaryBitmap = new BinaryBitmap(new HybridBinarizer(source));

            // Try single result first
            try
            {
                var result = _reader.decode(binaryBitmap);
                results.Add(CreateQrResult(result, source));
            }
            catch (ReaderException)
            {
                // Single result failed, try multiple
                try
                {
                    var multiResults = _reader.decodeMultiple(binaryBitmap);
                    foreach (var r in multiResults)
                    {
                        results.Add(CreateQrResult(r, source));
                    }
                }
                catch (ReaderException)
                {
                    // No QR found
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"QR decode error: {ex.Message}");
        }

        // If cursor position given, pick the closest or containing QR
        if (results.Count > 1 && cursorX.HasValue && cursorY.HasValue)
        {
            var cursor = (cursorX.Value, cursorY.Value);
            var containing = results.FirstOrDefault(r =>
                r.BoundingBox != null && r.BoundingBox.Value.Contains(cursorX.Value, cursorY.Value));

            if (containing != null)
                return new List<QrResult> { containing };

            var closest = results.MinBy(r =>
            {
                if (r.BoundingBox == null) return int.MaxValue;
                var box = r.BoundingBox.Value;
                var centerX = box.X + box.Width / 2;
                var centerY = box.Y + box.Height / 2;
                var dx = centerX - cursorX.Value;
                var dy = centerY - cursorY.Value;
                return (int)Math.Sqrt(dx * dx + dy * dy);
            });

            if (closest != null)
                return new List<QrResult> { closest };
        }

        return results;
    }

    private QrResult CreateQrResult(Result result, BitmapLuminanceSource source)
    {
        var payload = _parser.Parse(result.Text);

        // Add safety analysis for URLs
        if (payload.Type == PayloadType.Url)
        {
            payload.Safety = _safety.Analyze(payload.Content);
        }

        // Extract bounding box
        Rectangle? bbox = null;
        if (result.ResultPoints != null && result.ResultPoints.Length >= 2)
        {
            bbox = ExtractBoundingBox(result.ResultPoints);
        }

        return new QrResult
        {
            RawContent = result.Text,
            Payload = payload,
            BoundingBox = bbox
        };
    }

    private static Rectangle ExtractBoundingBox(ResultPoint[] points)
    {
        if (points.Length == 0)
            return default;

        var minX = (int)points.Min(p => p.X);
        var maxX = (int)points.Max(p => p.X);
        var minY = (int)points.Min(p => p.Y);
        var maxY = (int)points.Max(p => p.Y);

        return new Rectangle
        {
            X = minX,
            Y = minY,
            Width = maxX - minX + 1,
            Height = maxY - minY + 1
        };
    }
}
