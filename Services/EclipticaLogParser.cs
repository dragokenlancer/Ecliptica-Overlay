using System.Globalization;
using System.Text.RegularExpressions;
using EclipticaOverlay.Models;

namespace EclipticaOverlay.Services;

/// Parses ECLIPTICA world log lines (as written to the VRChat output log) into MatchState updates.
public sealed partial class EclipticaLogParser
{
    private static readonly Regex LinePrefix = MyLinePrefixRegex();
    private static readonly Regex SessionLoaded = MySessionLoadedRegex();
    private static readonly Regex BlankSession = MyBlankSessionRegex();
    private static readonly Regex SessionSaved = MySessionSavedRegex();
    private static readonly Regex NowInStage = MyNowInStageRegex();
    private static readonly Regex NowFightingBoss = MyNowFightingBossRegex();
    private static readonly Regex NowInIntermission = MyNowInIntermissionRegex();
    private static readonly Regex NowInLobby = MyNowInLobbyRegex();
    private static readonly Regex OwnershipTransfer = MyOwnershipTransferRegex();
    private static readonly Regex DealingDamage = MyDealingDamageRegex();
    private static readonly Regex DamageTaken = MyDamageTakenRegex();
    private static readonly Regex BossDeadPersonal = MyBossDeadPersonalRegex();
    private static readonly Regex StrikeDmgTotal = MyStrikeDmgTotalRegex();
    private static readonly Regex NonStrikeDmgTotal = MyNonStrikeDmgTotalRegex();

    // Tracks an in-progress "boss died" line sequence (Boss X dead... / STRIKE DMG: n /
    // NON-STRIKE DMG: n arrive as three consecutive lines) so the final tally can be attributed
    // to the right boss. Scoped to this parser instance, i.e. to one log session.
    private string? _pendingKillBoss;
    private int _pendingKillStrikeDmg;

    /// Attempts to interpret a raw VRChat log line. Returns true and mutates `state` if the
    /// line carried info this app tracks; returns false (state untouched) otherwise.
    public bool TryApply(string rawLine, MatchState state)
    {
        var prefixMatch = LinePrefix.Match(rawLine);
        if (!prefixMatch.Success)
            return false;

        var timestamp = DateTime.ParseExact(
            prefixMatch.Groups["ts"].Value, "yyyy.MM.dd HH:mm:ss",
            CultureInfo.InvariantCulture);
        var message = prefixMatch.Groups["msg"].Value;

        return message.StartsWith("ECLIPTICA", StringComparison.Ordinal)
            ? ApplyEclipticaLine(message, timestamp, state)
            : ApplyCombatLine(message, timestamp, state);
    }

    private bool ApplyEclipticaLine(string message, DateTime timestamp, MatchState state)
    {
        Match m;
        if ((m = NowInStage.Match(message)).Success)
        {
            state.Status = RunStatus.Stage;
            state.StageName = CleanStageName(m.Groups["stage"].Value);
            state.PhaseProgress = double.Parse(m.Groups["phase"].Value, CultureInfo.InvariantCulture);
            state.PlayerClass = m.Groups["class"].Value.Trim();
            state.BossName = null;
            state.SegmentStartedAt = timestamp;
            ResetSegmentStats(state);
        }
        else if ((m = NowFightingBoss.Match(message)).Success)
        {
            state.Status = RunStatus.BossFight;
            state.BossName = m.Groups["boss"].Value.Trim();
            state.PhaseProgress = double.Parse(m.Groups["phase"].Value, CultureInfo.InvariantCulture);
            state.SegmentStartedAt = timestamp;

            // Drop leftover aggro from whatever was fought earlier this stage — keep only the
            // boss that's now starting, if it happens to already have an entry.
            state.EnemyAggro = state.EnemyAggro.TryGetValue(state.BossName, out var existing)
                ? new Dictionary<string, EnemyAggroEntry> { [state.BossName] = existing }
                : new Dictionary<string, EnemyAggroEntry>();
        }
        else if (NowInIntermission.IsMatch(message))
        {
            state.Status = RunStatus.Intermission;
            state.BossName = null;
            state.SegmentStartedAt = timestamp;
            state.EnemyAggro = new Dictionary<string, EnemyAggroEntry>();
        }
        else if (NowInLobby.IsMatch(message))
        {
            state.Status = RunStatus.Lobby;
            state.StageName = null;
            state.BossName = null;
            state.PlayerClass = null;
            state.PhaseProgress = null;
            state.SegmentStartedAt = timestamp;
            ResetSegmentStats(state);
        }
        else if ((m = SessionLoaded.Match(message)).Success)
        {
            state.SessionId = m.Groups["id"].Value;
        }
        else if (BlankSession.IsMatch(message))
        {
            state.SessionId = null;
        }
        else if ((m = SessionSaved.Match(message)).Success)
        {
            state.SessionId = m.Groups["id"].Value;
        }
        else
        {
            // Recognized as an ECLIPTICA line (e.g. "Loading Settings...", "successfully loaded
            // SESSION data.") but carries no state we track.
        }

        return true;
    }

    private bool ApplyCombatLine(string message, DateTime timestamp, MatchState state)
    {
        Match m;
        if ((m = OwnershipTransfer.Match(message)).Success)
        {
            var enemy = m.Groups["enemy"].Value.Trim();
            var player = m.Groups["player"].Value.Trim();
            var updated = new Dictionary<string, EnemyAggroEntry>(state.EnemyAggro)
            {
                [enemy] = new EnemyAggroEntry(player, timestamp)
            };
            state.EnemyAggro = updated;
        }
        else if ((m = DealingDamage.Match(message)).Success)
        {
            var amount = int.Parse(m.Groups["amt"].Value, CultureInfo.InvariantCulture);
            if (m.Groups["kind"].Value == "STRIKE")
                state.DamageDealtStrike += amount;
            else
                state.DamageDealtNonStrike += amount;
        }
        else if ((m = DamageTaken.Match(message)).Success)
        {
            var amount = int.Parse(m.Groups["amt"].Value, CultureInfo.InvariantCulture);
            state.DamageTakenTotal += amount;
            state.LastHitSource = m.Groups["src"].Success
                ? m.Groups["src"].Value.Trim()
                : m.Groups["atk"].Value.Trim();
            state.LastHitAmount = amount;
            state.LastHitAt = timestamp;
        }
        else if ((m = BossDeadPersonal.Match(message)).Success)
        {
            _pendingKillBoss = m.Groups["name"].Value.Trim();
            _pendingKillStrikeDmg = 0;

            if (state.EnemyAggro.ContainsKey(_pendingKillBoss))
            {
                var withoutBoss = new Dictionary<string, EnemyAggroEntry>(state.EnemyAggro);
                withoutBoss.Remove(_pendingKillBoss);
                state.EnemyAggro = withoutBoss;
            }
        }
        else if ((m = StrikeDmgTotal.Match(message)).Success && _pendingKillBoss != null)
        {
            _pendingKillStrikeDmg = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
        }
        else if ((m = NonStrikeDmgTotal.Match(message)).Success && _pendingKillBoss != null)
        {
            var nonStrike = int.Parse(m.Groups["n"].Value, CultureInfo.InvariantCulture);
            if (_pendingKillStrikeDmg + nonStrike > 0)
            {
                state.LastDefeatedBoss = _pendingKillBoss;
                state.LastDefeatedStrikeDmg = _pendingKillStrikeDmg;
                state.LastDefeatedNonStrikeDmg = nonStrike;
                state.LastDefeatedAt = timestamp;
            }
            _pendingKillBoss = null;
        }
        else
        {
            return false;
        }

        return true;
    }

    private static void ResetSegmentStats(MatchState state)
    {
        state.DamageDealtStrike = 0;
        state.DamageDealtNonStrike = 0;
        state.DamageTakenTotal = 0;
        state.LastHitSource = null;
        state.LastHitAmount = null;
        state.LastHitAt = null;
        state.EnemyAggro = new Dictionary<string, EnemyAggroEntry>();
    }

    // Word-boundary splitter for internal PascalCase/acronym names, e.g. "CopiedCity" ->
    // "Copied City", "GMFuncFlat3" -> "GM Func Flat 3". Names that already contain spaces
    // (e.g. "Hall of Beginnings") pass through unchanged since there's no case transition to split on.
    private static readonly Regex WordBoundary = MyWordBoundaryRegex();

    private static string CleanStageName(string raw)
    {
        var name = raw.StartsWith("Stage_", StringComparison.Ordinal) ? raw["Stage_".Length..] : raw;
        return WordBoundary.Replace(name, " ");
    }

    [GeneratedRegex(@"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[a-zA-Z])(?=[0-9])")]
    private static partial Regex MyWordBoundaryRegex();

    [GeneratedRegex(@"^(?<ts>\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2})\s+\S+\s+-\s+(?<msg>.*)$")]
    private static partial Regex MyLinePrefixRegex();

    [GeneratedRegex(@"^ECLIPTICA loaded SESSION ID (?<id>\d+)")]
    private static partial Regex MySessionLoadedRegex();

    [GeneratedRegex(@"^ECLIPTICA loaded blank session ID\.")]
    private static partial Regex MyBlankSessionRegex();

    [GeneratedRegex(@"^ECLIPTICA saving SESSION ID (?<id>\d+)")]
    private static partial Regex MySessionSavedRegex();

    [GeneratedRegex(@"^ECLIPTICA - now in stage: (?<stage>.+?) on phase: (?<phase>[\d.]+) as class: (?<class>.+)$")]
    private static partial Regex MyNowInStageRegex();

    [GeneratedRegex(@"^ECLIPTICA - now fighting boss: (?<boss>.+?)(\(Clone\))? on phase: (?<phase>[\d.]+)$")]
    private static partial Regex MyNowFightingBossRegex();

    [GeneratedRegex(@"^ECLIPTICA - now in intermission")]
    private static partial Regex MyNowInIntermissionRegex();

    [GeneratedRegex(@"^ECLIPTICA - now in lobby")]
    private static partial Regex MyNowInLobbyRegex();

    [GeneratedRegex(@"^ownership of (?<enemy>.+?) transferred to (?<player>.+)$")]
    private static partial Regex MyOwnershipTransferRegex();

    [GeneratedRegex(@"^Dealing (?<amt>\d+) (?<kind>STRIKE|NON-STRIKE) damage$")]
    private static partial Regex MyDealingDamageRegex();

    [GeneratedRegex(@"^damage has been taken: (?<amt>\d+), from source: (\((?<src>[^)]+)\) ?)?(?<atk>.*)$")]
    private static partial Regex MyDamageTakenRegex();

    [GeneratedRegex(@"^Boss (?<name>.+) dead, personal damage dealt:\s*$")]
    private static partial Regex MyBossDeadPersonalRegex();

    [GeneratedRegex(@"^STRIKE DMG: (?<n>\d+)$")]
    private static partial Regex MyStrikeDmgTotalRegex();

    [GeneratedRegex(@"^NON-STRIKE DMG: (?<n>\d+)$")]
    private static partial Regex MyNonStrikeDmgTotalRegex();
}
