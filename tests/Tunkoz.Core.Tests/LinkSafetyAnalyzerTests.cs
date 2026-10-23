using Xunit;
using Tunkoz.Core;
using Tunkoz.Core.Models;

namespace Tunkoz.Core.Tests;

public class LinkSafetyAnalyzerTests
{
    private readonly LinkSafetyAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_HttpsUrl_ReturnsSafe()
    {
        var analysis = _analyzer.Analyze("https://example.com");

        Assert.Equal(SafetySeverity.Safe, analysis.Severity);
        Assert.Equal("example.com", analysis.RegistrableDomain);
        Assert.Null(analysis.ReasonKey);
    }

    [Fact]
    public void Analyze_PlainHttpUrl_ReturnsWarning()
    {
        var analysis = _analyzer.Analyze("http://example.com");

        Assert.Equal(SafetySeverity.Warning, analysis.Severity);
        Assert.Equal("plain_http", analysis.ReasonKey);
    }

    [Fact]
    public void Analyze_JavaScriptUrl_ReturnsBlocked()
    {
        var analysis = _analyzer.Analyze("javascript:alert('xss')");

        Assert.Equal(SafetySeverity.Blocked, analysis.Severity);
        Assert.Equal("blocked_scheme", analysis.ReasonKey);
    }

    [Fact]
    public void Analyze_DataUrl_ReturnsBlocked()
    {
        var analysis = _analyzer.Analyze("data:text/html,<script>alert('xss')</script>");

        Assert.Equal(SafetySeverity.Blocked, analysis.Severity);
    }

    [Fact]
    public void Analyze_FileUrl_ReturnsBlocked()
    {
        var analysis = _analyzer.Analyze("file:///etc/passwd");

        Assert.Equal(SafetySeverity.Blocked, analysis.Severity);
    }

    [Fact]
    public void Analyze_KnownShortener_ReturnsWarning()
    {
        var analysis = _analyzer.Analyze("https://bit.ly/2K9bX8Y");

        Assert.Equal(SafetySeverity.Warning, analysis.Severity);
        Assert.Equal("known_shortener", analysis.ReasonKey);
        Assert.True(analysis.IsShortener);
    }

    [Fact]
    public void Analyze_IpAddress_ReturnsWarning()
    {
        var analysis = _analyzer.Analyze("https://192.168.1.1");

        Assert.Equal(SafetySeverity.Warning, analysis.Severity);
        Assert.Equal("raw_ip_host", analysis.ReasonKey);
    }

    [Fact]
    public void Analyze_PunycodeUrl_ReturnsWarningWithDecoded()
    {
        var analysis = _analyzer.Analyze("https://xn--e28h.com");

        Assert.Equal(SafetySeverity.Warning, analysis.Severity);
        Assert.Equal("punycode_domain", analysis.ReasonKey);
        Assert.NotNull(analysis.DecodedDisplay);
    }

    [Fact]
    public void Analyze_ExcessivelyLongUrl_ReturnsWarning()
    {
        var longPath = string.Concat(Enumerable.Repeat("a", 2500));
        var url = $"https://example.com/{longPath}";

        var analysis = _analyzer.Analyze(url);

        Assert.Equal(SafetySeverity.Warning, analysis.Severity);
        Assert.Equal("suspicious_pattern", analysis.ReasonKey);
    }

    [Fact]
    public void Analyze_InvalidUrl_ReturnsBlocked()
    {
        var analysis = _analyzer.Analyze("not a valid url at all !!!");

        Assert.Equal(SafetySeverity.Blocked, analysis.Severity);
        Assert.Equal("invalid_url", analysis.ReasonKey);
    }

    [Fact]
    public void Analyze_UrlWithComplexPath_ExtractsDomain()
    {
        var analysis = _analyzer.Analyze("https://subdomain.example.com/path/to/page?query=value#hash");

        Assert.Equal(SafetySeverity.Safe, analysis.Severity);
        Assert.Contains("example.com", analysis.RegistrableDomain);
    }

    [Fact]
    public void Analyze_MsSettingsScheme_ReturnsWarning()
    {
        var analysis = _analyzer.Analyze("ms-settings:privacy");

        Assert.Equal(SafetySeverity.Warning, analysis.Severity);
        Assert.Equal("dangerous_scheme", analysis.ReasonKey);
    }
}
