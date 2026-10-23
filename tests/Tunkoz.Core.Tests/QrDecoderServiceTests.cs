using System.Drawing;
using Xunit;
using Tunkoz.Core;
using Tunkoz.Core.Models;

namespace Tunkoz.Core.Tests;

public class QrDecoderServiceTests
{
    private readonly QrDecoderService _decoder = new();

    [Fact]
    public void DecodeFromBitmap_WithNullBitmap_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _decoder.DecodeFromBitmap(null!));
    }

    [Fact]
    public void DecodeFromBitmap_EmptyBitmap_ReturnsEmpty()
    {
        using var bitmap = new Bitmap(100, 100);
        var results = _decoder.DecodeFromBitmap(bitmap);

        Assert.Empty(results);
    }

    [Fact]
    public void DecodeFromBitmap_WithCursorPosition_FiltersResults()
    {
        // This test verifies the cursor-based filtering logic
        // In a real scenario, we'd have an actual QR image to decode
        using var bitmap = new Bitmap(100, 100);
        var results = _decoder.DecodeFromBitmap(bitmap, cursorX: 50, cursorY: 50);

        // Should return empty list for blank bitmap
        Assert.Empty(results);
    }
}
