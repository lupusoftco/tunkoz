using System.Drawing;
using ZXing;
using ZXing.Common;
using Tunkoz.Core.Models;

namespace Tunkoz.Core;

public class QrDecoderService : IDisposable
{
    private readonly BarcodeReader<Bitmap> _reader;
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

        var options = new DecodingOptions
        {
            TryHarder = true,
            PossibleFormats = new[] { BarcodeFormat.QR_CODE }
        };

        Func<Bitmap, LuminanceSource> bitmapToLuminance = bitmap =>
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var pixels = new byte[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    // Convert RGB to grayscale using standard formula
                    var gray = (byte)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                    pixels[y * width + x] = gray;
                }
            }

            return new RGBLuminanceSource(pixels, width, height, RGBLuminanceSource.BitmapFormat.Gray8);
        };

        _reader = new BarcodeReader<Bitmap>(bitmapToLuminance)
        {
            Options = options
        };
        _parser = parser ?? new PayloadParser();
        _safety = safety ?? new LinkSafetyAnalyzer();
    }

    public List<QrResult> DecodeFromBitmap(Bitmap bitmap, int? cursorX = null, int? cursorY = null)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var results = new List<QrResult>();

        try
        {
            // Try single result first
            var result = _reader.Decode(bitmap);
            if (result != null)
            {
                results.Add(CreateQrResult(result));
            }
            else
            {
                // Single result failed, try multiple
                var multiResults = _reader.DecodeMultiple(bitmap);
                if (multiResults != null)
                {
                    foreach (var r in multiResults)
                    {
                        results.Add(CreateQrResult(r));
                    }
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

    private QrResult CreateQrResult(Result result)
    {
        var payload = _parser.Parse(result.Text);

        // Add safety analysis for URLs
        if (payload.Type == PayloadType.Url)
        {
            payload.Safety = _safety.Analyze(payload.Content);
        }

        // Extract bounding box
        Models.Rectangle? bbox = null;
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

    private static Models.Rectangle ExtractBoundingBox(ResultPoint[] points)
    {
        if (points.Length == 0)
            return default;

        var minX = (int)points.Min(p => p.X);
        var maxX = (int)points.Max(p => p.X);
        var minY = (int)points.Min(p => p.Y);
        var maxY = (int)points.Max(p => p.Y);

        return new Models.Rectangle
        {
            X = minX,
            Y = minY,
            Width = maxX - minX + 1,
            Height = maxY - minY + 1
        };
    }

    public void Dispose()
    {
        // BarcodeReader doesn't need disposal, but keeping this for future cleanup if needed
    }
}
