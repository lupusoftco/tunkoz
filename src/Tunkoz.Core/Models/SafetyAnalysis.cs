namespace Tunkoz.Core.Models;

public class SafetyAnalysis
{
    public required SafetySeverity Severity { get; set; }
    public required string DisplayUrl { get; set; }
    public required string RegistrableDomain { get; set; }
    public string? ReasonKey { get; set; }
    public string? DecodedDisplay { get; set; }
    public bool IsShortener { get; set; }
}
