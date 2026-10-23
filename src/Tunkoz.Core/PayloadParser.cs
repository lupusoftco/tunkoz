using System.Text.RegularExpressions;
using Tunkoz.Core.Models;

namespace Tunkoz.Core;

public class PayloadParser
{
    public Payload Parse(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);

        return raw.ToUpperInvariant() switch
        {
            var s when s.StartsWith("WIFI:") => ParseWiFi(raw),
            var s when s.StartsWith("BEGIN:VCARD") || s.StartsWith("BEGIN:MECARD") => ParseVCard(raw),
            var s when s.StartsWith("TEL:") => ParseTel(raw),
            var s when s.StartsWith("MAILTO:") => ParseMailto(raw),
            var s when s.StartsWith("SMSTO:") => ParseSms(raw),
            var s when s.StartsWith("GEO:") => ParseGeo(raw),
            _ => ParseUrl(raw) ?? ParseText(raw)
        };
    }

    private Payload ParseUrl(string raw)
    {
        if (Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return new Payload
            {
                Type = PayloadType.Url,
                Content = uri.AbsoluteUri,
                Metadata = new Dictionary<string, string>
                {
                    { "scheme", uri.Scheme },
                    { "host", uri.Host },
                    { "port", uri.Port.ToString() }
                }
            };
        }

        // Try with http:// prefix
        if (Uri.TryCreate("http://" + raw, UriKind.Absolute, out uri) &&
            !raw.Contains("://") && !raw.StartsWith("/"))
        {
            return new Payload
            {
                Type = PayloadType.Url,
                Content = uri.AbsoluteUri,
                Metadata = new Dictionary<string, string>
                {
                    { "scheme", "http" },
                    { "host", uri.Host },
                    { "port", uri.Port.ToString() }
                }
            };
        }

        return null!;
    }

    private Payload ParseText(string raw)
    {
        return new Payload
        {
            Type = PayloadType.Text,
            Content = raw
        };
    }

    private Payload ParseWiFi(string raw)
    {
        var metadata = new Dictionary<string, string>();
        var match = Regex.Match(raw, @"WIFI:T:([^;]*);S:([^;]*);P:([^;]*);?");

        if (match.Success)
        {
            metadata["type"] = UnescapeWiFi(match.Groups[1].Value);
            metadata["ssid"] = UnescapeWiFi(match.Groups[2].Value);
            metadata["password"] = UnescapeWiFi(match.Groups[3].Value);
        }

        return new Payload
        {
            Type = PayloadType.WiFi,
            Content = $"{metadata.GetValueOrDefault("ssid", "Unknown Network")}",
            Metadata = metadata
        };
    }

    private static string UnescapeWiFi(string s)
    {
        return Regex.Replace(s, @"\\(.)", m => m.Groups[1].Value);
    }

    private Payload ParseVCard(string raw)
    {
        var metadata = new Dictionary<string, string>();
        var isMeCard = raw.StartsWith("MECARD:");

        if (isMeCard)
        {
            ParseMeCard(raw, metadata);
        }
        else
        {
            ParseVCard(raw, metadata);
        }

        var name = metadata.GetValueOrDefault("fn", metadata.GetValueOrDefault("n", "Contact"));
        return new Payload
        {
            Type = PayloadType.VCard,
            Content = name,
            Metadata = metadata
        };
    }

    private static void ParseMeCard(string raw, Dictionary<string, string> metadata)
    {
        var content = raw[7..].TrimEnd(';');
        var pairs = content.Split(';');

        foreach (var pair in pairs)
        {
            var idx = pair.IndexOf(':');
            if (idx < 0) continue;

            var key = pair[..idx].ToLowerInvariant();
            var value = pair[(idx + 1)..];

            metadata[key] = value;
        }
    }

    private static void ParseVCard(string raw, Dictionary<string, string> metadata)
    {
        var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var idx = line.IndexOf(':');
            if (idx < 0) continue;

            var key = line[..idx].ToLowerInvariant();
            var value = line[(idx + 1)..];

            if (key == "fn" || key == "n")
                metadata["fn"] = value;
            else if (key.StartsWith("tel"))
                metadata["tel"] = value;
            else if (key.StartsWith("email"))
                metadata["email"] = value;
        }
    }

    private Payload ParseTel(string raw)
    {
        var number = raw[4..];
        return new Payload
        {
            Type = PayloadType.Tel,
            Content = number,
            Metadata = new Dictionary<string, string> { { "number", number } }
        };
    }

    private Payload ParseMailto(string raw)
    {
        var address = raw[7..];
        return new Payload
        {
            Type = PayloadType.Mailto,
            Content = address,
            Metadata = new Dictionary<string, string> { { "address", address } }
        };
    }

    private Payload ParseSms(string raw)
    {
        var parts = raw[6..].Split('?');
        var number = parts[0];

        return new Payload
        {
            Type = PayloadType.Sms,
            Content = number,
            Metadata = new Dictionary<string, string> { { "number", number } }
        };
    }

    private Payload ParseGeo(string raw)
    {
        var coords = raw[4..].TrimEnd('?');
        return new Payload
        {
            Type = PayloadType.Geo,
            Content = coords,
            Metadata = new Dictionary<string, string> { { "coords", coords } }
        };
    }
}
