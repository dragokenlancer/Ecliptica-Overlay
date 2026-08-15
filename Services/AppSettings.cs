using System.IO;
using System.Text.Json;

namespace EclipticaOverlay.Services;

/// Small persisted settings blob, stored under %AppData%/EclipticaOverlay/settings.json so
/// toggles (like chatbox output) survive a restart.
public sealed class AppSettings
{
    public bool ChatboxEnabled { get; set; }
    public bool ChatboxNotifySound { get; set; }
    public double ChatboxIntervalSeconds { get; set; } = 1.5;

    // One template per game state, since the fields worth showing differ per state.
    // {Key} placeholders are substituted by ChatboxMessageBuilder — see AvailableKeys there.
    public string LobbyTemplate { get; set; } = "Ecliptica | Lobby";
    public string StageTemplate { get; set; } = "Ecliptica | {Stage} · {Tier} {Percent}%";
    public string BossTemplate { get; set; } = "Ecliptica | {Boss} · {Percent}% · Weak: {Weak} · Aggro: {Aggro}";
    public string IntermissionTemplate { get; set; } = "Ecliptica | Intermission";

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EclipticaOverlay", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                    return loaded;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Missing/corrupt/unreadable settings file — just fall back to defaults.
        }

        return new AppSettings();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
    }
}
