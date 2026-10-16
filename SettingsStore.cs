using System.IO;
using System.Text.Json;

namespace FiveMOverlay;

public static class SettingsStore
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveMOverlay");
    private static readonly string FilePath = Path.Combine(Folder, "settings.json");

    public static OverlaySettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new OverlaySettings();
            using var document = JsonDocument.Parse(File.ReadAllText(FilePath));
            var root = document.RootElement;
            // Migrate settings made by the older one-overlay version.
            if (!root.TryGetProperty("Overlay1", out _))
            {
                var legacy = JsonSerializer.Deserialize<OverlayProfile>(root.GetRawText()) ?? new OverlayProfile();
                return new OverlaySettings { Overlay1 = legacy };
            }
            return JsonSerializer.Deserialize<OverlaySettings>(root.GetRawText()) ?? new OverlaySettings();
        }
        catch { return new OverlaySettings(); }
    }

    public static void Save(OverlaySettings settings)
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
