namespace Tunkoz.Core.Models;

public class Payload
{
    public required PayloadType Type { get; set; }
    public required string Content { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public SafetyAnalysis? Safety { get; set; }

    public override string ToString() => $"{Type}: {Content}";
}
