using System.IO;
using System.Text.Json;

namespace Tunkoz.Services;

/// <summary>User settings persisted to %AppData%\Tunkoz\settings.json.</summary>
public sealed class AppSettings
{
    /// <summary>"system", "tr" or "en".</summary>
    public string Language { get; set; } = "system";
}

public static class SettingsStore
{
    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tunkoz");

    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    public static AppSettings Current { get; private set; } = new();

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
            Current = new AppSettings();
        }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Settings are a convenience; never crash over them
        }
    }

    /// <summary>Resolves the stored preference to an actual UI language.</summary>
    public static string EffectiveLanguage =>
        Current.Language is "tr" or "en" ? Current.Language : Loc.SystemDefaultLanguage;
}
