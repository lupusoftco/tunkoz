using Xunit;
using Tunkoz.Core;
using Tunkoz.Core.Models;

namespace Tunkoz.Core.Tests;

public class PayloadParserTests
{
    private readonly PayloadParser _parser = new();

    [Fact]
    public void Parse_ValidHttpUrl_ReturnsUrlPayload()
    {
        var payload = _parser.Parse("https://example.com");

        Assert.Equal(PayloadType.Url, payload.Type);
        Assert.Equal("https://example.com/", payload.Content);
        Assert.NotNull(payload.Metadata);
        Assert.Equal("https", payload.Metadata["scheme"]);
        Assert.Equal("example.com", payload.Metadata["host"]);
    }

    [Fact]
    public void Parse_UrlWithoutScheme_AddHttpScheme()
    {
        var payload = _parser.Parse("example.com");

        Assert.Equal(PayloadType.Url, payload.Type);
        Assert.Equal("http://example.com/", payload.Content);
    }

    [Fact]
    public void Parse_TelUrl_ReturnsTelPayload()
    {
        var payload = _parser.Parse("tel:+1234567890");

        Assert.Equal(PayloadType.Tel, payload.Type);
        Assert.Equal("+1234567890", payload.Content);
        Assert.Equal("+1234567890", payload.Metadata?["number"]);
    }

    [Fact]
    public void Parse_MailtoUrl_ReturnsMailtoPayload()
    {
        var payload = _parser.Parse("mailto:test@example.com");

        Assert.Equal(PayloadType.Mailto, payload.Type);
        Assert.Equal("test@example.com", payload.Content);
    }

    [Fact]
    public void Parse_SmsUrl_ReturnsSmsPayload()
    {
        var payload = _parser.Parse("smsto:+1234567890?body=Hello");

        Assert.Equal(PayloadType.Sms, payload.Type);
        Assert.Equal("+1234567890", payload.Content);
    }

    [Fact]
    public void Parse_GeoUrl_ReturnsGeoPayload()
    {
        var payload = _parser.Parse("geo:37.786971,-122.399677");

        Assert.Equal(PayloadType.Geo, payload.Type);
        Assert.Equal("37.786971,-122.399677", payload.Content);
    }

    [Fact]
    public void Parse_WiFiQr_ReturnsWiFiPayload()
    {
        var payload = _parser.Parse("WIFI:T:WPA;S:MyNetwork;P:MyPassword;");

        Assert.Equal(PayloadType.WiFi, payload.Type);
        Assert.Equal("MyNetwork", payload.Content);
        Assert.NotNull(payload.Metadata);
        Assert.Equal("MyNetwork", payload.Metadata["ssid"]);
        Assert.Equal("MyPassword", payload.Metadata["password"]);
    }

    [Fact]
    public void Parse_VCard_ReturnsVCardPayload()
    {
        var vcard = "BEGIN:VCARD\r\nVERSION:3.0\r\nFN:John Doe\r\nTEL:+1234567890\r\nEND:VCARD";
        var payload = _parser.Parse(vcard);

        Assert.Equal(PayloadType.VCard, payload.Type);
        Assert.Equal("John Doe", payload.Content);
        Assert.NotNull(payload.Metadata);
        Assert.Equal("+1234567890", payload.Metadata["tel"]);
    }

    [Fact]
    public void Parse_MeCard_ReturnsVCardPayload()
    {
        var payload = _parser.Parse("MECARD:N:Doe,John;TEL:+1234567890;EMAIL:john@example.com;");

        Assert.Equal(PayloadType.VCard, payload.Type);
    }

    [Fact]
    public void Parse_PlainText_ReturnsTextPayload()
    {
        var payload = _parser.Parse("Hello, World!");

        Assert.Equal(PayloadType.Text, payload.Type);
        Assert.Equal("Hello, World!", payload.Content);
    }

    [Fact]
    public void Parse_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _parser.Parse(""));
    }

    [Fact]
    public void Parse_HttpsUrlWithPath_PreservesPath()
    {
        var payload = _parser.Parse("https://example.com/path/to/page?query=value");

        Assert.Equal(PayloadType.Url, payload.Type);
        Assert.Contains("example.com", payload.Content);
        Assert.Contains("path", payload.Content);
    }
}
