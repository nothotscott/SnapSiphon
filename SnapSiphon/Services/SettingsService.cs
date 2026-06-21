using System.Text.Json;

namespace SnapSiphon.Services;

public sealed class AppSettings
{
    public string OutputFolderName { get; set; } = "output";
    public string FilePrefix { get; set; } = "Snapchat-";
    public string LastInputPath { get; set; } = "";
}

public static class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SnapSiphon");

    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new();
            }
        }
        catch { /* corrupt or inaccessible — fall through to defaults */ }
        return new();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }
        catch { /* non-critical — settings just won't persist */ }
    }
}
