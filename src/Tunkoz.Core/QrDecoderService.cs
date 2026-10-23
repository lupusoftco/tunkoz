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
        var options = new DecodingOptions
        {
            TryHarder = true,
            TryInverted = true,
            PossibleFormats = new[] { BarcodeFormat.QR_CODE }
        };

        // Fast grayscale conversion via LockBits (GetPixel is far too slow for a 500x500 capture)
        Func<Bitmap, LuminanceSource> bitmapToLuminance = bitmap =>
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
            var rect = new System.Drawing.Rectangle(0, 0, width, height);
            var data = bitmap.LockBits(rect,
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                var stride = Math.Abs(data.Stride);
                var raw = new byte[stride * height];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, raw, 0, raw.Length);

                var gray = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    var row = y * stride;
                    var outRow = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        var i = row + x * 4; // BGRA
                        var b = raw[i];
                        var g = raw[i + 1];
                        var r = raw[i + 2];
                        var a = raw[i + 3];
                        var lum = (r * 299 + g * 587 + b * 114) / 1000;
                        // Treat transparent pixels as white (common for QR PNGs)
                        lum = (lum * a + 255 * (255 - a)) / 255;
                        gray[outRow + x] = (byte)lum;
                    }
                }

                return new RGBLuminanceSource(gray, width, height, RGBLuminanceSource.BitmapFormat.Gray8);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
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
