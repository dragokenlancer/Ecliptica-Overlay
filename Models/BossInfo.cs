namespace EclipticaOverlay.Models;

/// Static reference info for a boss, sourced from https://ecliptica.miraheze.org/wiki.
/// Hand-mapped from the log's internal boss name to the wiki page it corresponds to, since
/// the two don't always match (e.g. log "FlyLord" is wiki page "Beelzebub"). Only bosses
/// that could be confidently identified on the wiki are included; anything else is simply
/// not shown rather than guessed.
/// No HP figure is included: the wiki's "Base HP" is a solo/unscaled number and doesn't
/// reflect the actual in-run value, which scales with something (players and/or difficulty)
/// that isn't documented anywhere — showing it would just be wrong.
public sealed record BossInfo(
    string Title,
    string DamageType,
    int Phases,
    string Strategy,
    // Short callout for the HP-threshold (or other) trigger that starts the next phase / grants
    // new attacks, e.g. "New attacks at 50% HP". Null when the wiki doesn't document one (either
    // the boss has no phase transition, or the trigger isn't %-based / wasn't specified).
    string? PhaseTrigger = null,
    // Elements this boss takes extra damage from, e.g. "Fire, Shadow". Null if none/all neutral.
    string? WeakTo = null,
    // Elements this boss takes reduced damage from, e.g. "Frost, Luminous". Null if none/all neutral.
    string? ResistTo = null);
