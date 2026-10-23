using System.Drawing;
using ZXing;
using ZXing.QrCode;

namespace Tunkoz.Core.Tests.Helpers;

/// <summary>
/// Generates QR code images for testing purposes.
/// </summary>
public static class QrImageGenerator
{
    public static Bitmap GenerateQrCode(string content, int width = 200, int height = 200)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = height,
                Width = width,
                Margin = 1
            }
        };

        var pixelData = writer.Write(content);
        var bitmap = new Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

        var data = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            System.Drawing.Imaging.ImageLockMode.WriteOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppRgb);

        System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, data.Scan0, pixelData.Pixels.Length);
        bitmap.UnlockBits(data);

        return bitmap;
    }

    public static void SaveSampleQrCodes(string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        var samples = new Dictionary<string, string>
        {
            { "url.png", "https://github.com/anthropics/claude-code" },
            { "email.png", "mailto:test@example.com" },
            { "phone.png", "tel:+1234567890" },
            { "wifi.png", "WIFI:T:WPA;S:TestNetwork;P:TestPassword;" },
            { "text.png", "Hello World" },
            { "vcard.png", "BEGIN:VCARD\r\nVERSION:3.0\r\nFN:John Doe\r\nTEL:+1234567890\r\nEND:VCARD" }
        };

        foreach (var (filename, content) in samples)
        {
            using var bitmap = GenerateQrCode(content);
            bitmap.Save(Path.Combine(outputDirectory, filename));
        }
    }
}
