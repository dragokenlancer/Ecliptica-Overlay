using System.Text;
using EclipticaOverlay.Models;

namespace EclipticaOverlay.Services;

/// Renders the chatbox text by substituting {Key} placeholders (ToNSaveManager-style) into
/// the per-state template from AppSettings, using the same fields the overlay UI shows (see
/// MainWindow.RefreshBossInfo / RefreshAggro). Trimmed to fit VRChat's 144-character limit.
public static class ChatboxMessageBuilder
{
    private const int MaxLength = 144;

    /// Keys available for use in a template, shown to the user in the settings window.
    public static readonly (string Key, string Description)[] AvailableKeys =
    {
        ("{Status}", "Current status: Lobby / Stage / Boss Fight / Intermission"),
        ("{Stage}", "Current stage name"),
        ("{Tier}", "Difficulty tier: Prime / Penumbra / Antumbra / Umbra / Eclipse"),
        ("{Percent}", "Stage/boss progress, 0-100"),
        ("{Boss}", "Boss title from the wiki reference, falls back to its raw log name"),
        ("{BossRaw}", "Boss's raw internal log name"),
        ("{Weak}", "Elements the boss is weak to (blank if unknown/none)"),
        ("{Resist}", "Elements the boss resists (blank if unknown/none)"),
        ("{PhaseTrigger}", "Callout for the boss's next phase trigger (blank if none)"),
        ("{Aggro}", "Current aggro holder and hold time, e.g. \"Bob (12s)\""),
        ("{AggroPlayer}", "Just the current aggro holder's name"),
        ("{AggroSeconds}", "Seconds the current holder has held aggro"),
        ("{Class}", "Your current in-game class"),
        ("{Elapsed}", "Time elapsed in the current stage/fight"),
        ("{DamageDealt}", "Your total damage dealt this stage"),
        ("{DamageTaken}", "Your total damage taken this stage"),
    };

    /// Returns null when there's nothing worth sending (log not connected, or the state's
    /// template is empty).
    public static string? Build(MatchState state, AppSettings settings)
    {
        if (!state.LogConnected)
            return null;

        var template = state.Status switch
        {
            RunStatus.Lobby => settings.LobbyTemplate,
            RunStatus.Stage => settings.StageTemplate,
            RunStatus.BossFight => settings.BossTemplate,
            RunStatus.Intermission => settings.IntermissionTemplate,
            _ => null,
        };

        if (string.IsNullOrEmpty(template))
            return null;

        var text = Render(template, state);
        return text.Length > MaxLength ? text[..MaxLength] : text;
    }

    private static string Render(string template, MatchState state)
    {
        var hasInfo = BossReference.TryGet(state.BossName, out var info);

        var aggroPlayer = "";
        var aggroSeconds = "";
        var aggro = "";
        if (state.BossName is { Length: > 0 } boss && state.EnemyAggro.TryGetValue(boss, out var entry))
        {
            var secs = Math.Max(0, (int)(DateTime.Now - entry.Since).TotalSeconds);
            aggroPlayer = entry.Player;
            aggroSeconds = secs.ToString();
            aggro = $"{entry.Player} ({secs}s)";
        }

        var elapsed = "";
        if (state.SegmentStartedAt is { } startedAt)
        {
            var span = DateTime.Now - startedAt;
            if (span < TimeSpan.Zero) span = TimeSpan.Zero;
            elapsed = span.TotalHours >= 1 ? span.ToString(@"h\:mm\:ss") : span.ToString(@"mm\:ss");
        }

        var values = new Dictionary<string, string>
        {
            ["Status"] = state.Status switch
            {
                RunStatus.Lobby => "Lobby",
                RunStatus.Stage => "Stage",
                RunStatus.BossFight => "Boss Fight",
                RunStatus.Intermission => "Intermission",
                _ => "",
            },
            ["Stage"] = state.StageName ?? "",
            ["Tier"] = state.PhaseProgress is { } tp ? DifficultyTier.Name(tp) : "",
            ["Percent"] = state.PhaseProgress is { } pp ? Math.Clamp(pp * 100.0, 0, 100).ToString("0") : "",
            ["Boss"] = hasInfo ? info.Title : (state.BossName ?? ""),
            ["BossRaw"] = state.BossName ?? "",
            ["Weak"] = hasInfo ? (info.WeakTo ?? "") : "",
            ["Resist"] = hasInfo ? (info.ResistTo ?? "") : "",
            ["PhaseTrigger"] = hasInfo ? (info.PhaseTrigger ?? "") : "",
            ["Aggro"] = aggro,
            ["AggroPlayer"] = aggroPlayer,
            ["AggroSeconds"] = aggroSeconds,
            ["Class"] = state.PlayerClass ?? "",
            ["Elapsed"] = elapsed,
            ["DamageDealt"] = (state.DamageDealtStrike + state.DamageDealtNonStrike).ToString(),
            ["DamageTaken"] = state.DamageTakenTotal.ToString(),
        };

        var sb = new StringBuilder(template);
        foreach (var (key, value) in values)
            sb.Replace("{" + key + "}", value);
        return sb.ToString();
    }
}
