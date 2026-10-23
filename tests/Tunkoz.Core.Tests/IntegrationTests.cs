using System.Drawing;
using Xunit;
using Tunkoz.Core;
using Tunkoz.Core.Models;
using Tunkoz.Core.Tests.Fixtures;
using Tunkoz.Core.Tests.Helpers;

namespace Tunkoz.Core.Tests;

[Collection("Sample QR Collection")]
public class IntegrationTests : IDisposable
{
    private readonly SampleQrFixture _fixture;
    private readonly QrDecoderService _decoder;

    public IntegrationTests(SampleQrFixture fixture)
    {
        _fixture = fixture;
        _decoder = new QrDecoderService();
    }

    [Fact]
    public void DecodeUrlQr_ReturnsUrlPayloadWithSafetyAnalysis()
    {
        using var bitmap = _fixture.GetSampleQr("url.png");
        var results = _decoder.DecodeFromBitmap(bitmap);

        Assert.NotEmpty(results);
        var result = results[0];
        Assert.Equal(PayloadType.Url, result.Payload.Type);
        Assert.NotNull(result.Payload.Safety);
        Assert.Equal(SafetySeverity.Safe, result.Payload.Safety.Severity);
    }

    [Fact]
    public void DecodeEmailQr_ReturnsMailtoPayload()
    {
        using var bitmap = _fixture.GetSampleQr("email.png");
        var results = _decoder.DecodeFromBitmap(bitmap);

        Assert.NotEmpty(results);
        var result = results[0];
        Assert.Equal(PayloadType.Mailto, result.Payload.Type);
        Assert.Contains("test@example.com", result.Payload.Content);
    }

    [Fact]
    public void DecodePhoneQr_ReturnsTelPayload()
    {
        using var bitmap = _fixture.GetSampleQr("phone.png");
        var results = _decoder.DecodeFromBitmap(bitmap);

        Assert.NotEmpty(results);
        var result = results[0];
        Assert.Equal(PayloadType.Tel, result.Payload.Type);
    }

    [Fact]
    public void DecodeWiFiQr_ReturnsWiFiPayload()
    {
        using var bitmap = _fixture.GetSampleQr("wifi.png");
        var results = _decoder.DecodeFromBitmap(bitmap);

        Assert.NotEmpty(results);
        var result = results[0];
        Assert.Equal(PayloadType.WiFi, result.Payload.Type);
        Assert.NotNull(result.Payload.Metadata);
        Assert.Equal("TestNetwork", result.Payload.Metadata["ssid"]);
    }

    [Fact]
    public void DecodeTextQr_ReturnsTextPayload()
    {
        using var bitmap = _fixture.GetSampleQr("text.png");
        var results = _decoder.DecodeFromBitmap(bitmap);

        Assert.NotEmpty(results);
        var result = results[0];
        Assert.Equal(PayloadType.Text, result.Payload.Type);
        Assert.Equal("Hello World", result.Payload.Content);
    }

    [Fact]
    public void DecodeVCardQr_ReturnsVCardPayload()
    {
        using var bitmap = _fixture.GetSampleQr("vcard.png");
        var results = _decoder.DecodeFromBitmap(bitmap);

        Assert.NotEmpty(results);
        var result = results[0];
        Assert.Equal(PayloadType.VCard, result.Payload.Type);
    }

    [Fact]
    public void DecodedQrHasBoundingBox()
    {
        using var bitmap = _fixture.GetSampleQr("text.png");
        var results = _decoder.DecodeFromBitmap(bitmap);

        Assert.NotEmpty(results);
        var result = results[0];
        Assert.NotNull(result.BoundingBox);
        Assert.True(result.BoundingBox.Value.Width > 0);
        Assert.True(result.BoundingBox.Value.Height > 0);
    }

    public void Dispose()
    {
        _decoder?.Dispose();
        GC.Collect();
    }
}
