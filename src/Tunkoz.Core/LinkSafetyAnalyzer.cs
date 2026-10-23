using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Tunkoz.Core.Models;

namespace Tunkoz.Core;

public class LinkSafetyAnalyzer
{
    private static readonly HashSet<string> KnownShorteners = new()
    {
        "bit.ly", "tinyurl.com", "ow.ly", "t.co", "short.link", "adf.ly",
        "buff.ly", "tiny.cc", "goo.gl", "s.id", "click.linksynergy.com"
    };

    private static readonly HashSet<string> BlockedSchemes = new()
    {
        "javascript", "data", "file", "vbscript"
    };

    private static readonly HashSet<string> DangerousSchemes = new()
    {
        "ms-appinstaller", "ms-settings", "ms-windows-store", "ms-help"
    };

    public SafetyAnalysis Analyze(string urlString)
    {
        if (!Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
        {
            return CreateBlocked("invalid_url");
        }

        // Check scheme
        var scheme = uri.Scheme.ToLowerInvariant();
        if (BlockedSchemes.Contains(scheme))
        {
            return CreateBlocked("blocked_scheme", displayUrl: urlString);
        }

        if (DangerousSchemes.Contains(scheme))
        {
            return CreateWarning("dangerous_scheme", displayUrl: urlString);
        }

        if (!scheme.StartsWith("http"))
        {
            return CreateWarning("non_http_scheme", displayUrl: urlString);
        }

        // Plain http warning
        var isSafe = scheme == "https";
        var severity = SafetySeverity.Safe;
        var reasonKey = "";

        if (!isSafe)
        {
            severity = SafetySeverity.Warning;
            reasonKey = "plain_http";
        }

        // Extract registrable domain
        var registrableDomain = ExtractRegistrableDomain(uri.Host);
        var displayUrl = uri.AbsoluteUri;

        // Check for punycode/IDN
        string? decodedDisplay = null;
        if (uri.Host.StartsWith("xn--"))
        {
            try
            {
                var idn = new IdnMapping();
                var decoded = idn.GetUnicode(uri.Host);
                decodedDisplay = decoded;
                severity = SafetySeverity.Warning;
                reasonKey = "punycode_domain";
            }
            catch { }
        }

        // Check for IP address
        if (IPAddress.TryParse(uri.Host, out _))
        {
            severity = SafetySeverity.Warning;
            reasonKey = "raw_ip_host";
        }

        // Check for shortener
        var isShortener = false;
        if (KnownShorteners.Contains(registrableDomain))
        {
            severity = SafetySeverity.Warning;
            reasonKey = "known_shortener";
            isShortener = true;
        }

        // Check for suspicious patterns in URL
        if (CheckSuspiciousPatterns(displayUrl))
        {
            severity = SafetySeverity.Warning;
            reasonKey = "suspicious_pattern";
        }

        return new SafetyAnalysis
        {
            Severity = severity,
            DisplayUrl = displayUrl,
            RegistrableDomain = registrableDomain,
            ReasonKey = reasonKey,
            DecodedDisplay = decodedDisplay,
            IsShortener = isShortener
        };
    }

    private static SafetyAnalysis CreateWarning(string reasonKey, string displayUrl = "")
    {
        return new SafetyAnalysis
        {
            Severity = SafetySeverity.Warning,
            DisplayUrl = displayUrl,
            RegistrableDomain = ExtractRegistrableDomain(displayUrl),
            ReasonKey = reasonKey,
            IsShortener = false
        };
    }

    private static SafetyAnalysis CreateBlocked(string reasonKey, string displayUrl = "")
    {
        return new SafetyAnalysis
        {
            Severity = SafetySeverity.Blocked,
            DisplayUrl = displayUrl,
            RegistrableDomain = ExtractRegistrableDomain(displayUrl),
            ReasonKey = reasonKey,
            IsShortener = false
        };
    }

    private static string ExtractRegistrableDomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return host ?? "";

        host = host.ToLowerInvariant();

        // Handle IP addresses
        if (IPAddress.TryParse(host, out _))
            return host;

        // Simple registrable domain extraction (full domain for now)
        // A production version would use the Public Suffix List
        var parts = host.Split('.');
        if (parts.Length <= 2)
            return host;

        // Return last 2 parts (domain.tld)
        return string.Join(".", parts[^2..]);
    }

    private static bool CheckSuspiciousPatterns(string url)
    {
        if (url.Length > 2000)
            return true;

        // Multiple redirects encoded in path
        if (Regex.IsMatch(url, @"[?&].*[?&].*[?&]", RegexOptions.IgnoreCase))
            return true;

        // Excessive percent-encoding
        var percentCount = url.Count(c => c == '%');
        if (percentCount > url.Length / 10)
            return true;

        return false;
    }
}
