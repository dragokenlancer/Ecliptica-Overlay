namespace EclipticaOverlay.Models;

public enum RunStatus
{
    Unknown,
    Lobby,
    Stage,
    BossFight,
    Intermission
}

public readonly record struct EnemyAggroEntry(string Player, DateTime Since);

public sealed class MatchState
{
    public RunStatus Status { get; set; } = RunStatus.Unknown;
    public string? StageName { get; set; }
    public string? BossName { get; set; }
    public string? PlayerClass { get; set; }
    public double? PhaseProgress { get; set; }
    public string? SessionId { get; set; }
    public DateTime? SegmentStartedAt { get; set; }
    public bool LogConnected { get; set; }

    // Who each currently-tracked enemy (boss or trash mob) has last handed authority to,
    // i.e. its live aggro target. Keyed by enemy display name. Copy-on-write.
    public IReadOnlyDictionary<string, EnemyAggroEntry> EnemyAggro { get; set; } =
        new Dictionary<string, EnemyAggroEntry>();

    // Running totals of the local player's own damage dealt/taken this stage.
    public int DamageDealtStrike { get; set; }
    public int DamageDealtNonStrike { get; set; }
    public int DamageTakenTotal { get; set; }
    public string? LastHitSource { get; set; }
    public int? LastHitAmount { get; set; }
    public DateTime? LastHitAt { get; set; }

    // Most recent boss kill, for a fading "defeated" toast.
    public string? LastDefeatedBoss { get; set; }
    public int? LastDefeatedStrikeDmg { get; set; }
    public int? LastDefeatedNonStrikeDmg { get; set; }
    public DateTime? LastDefeatedAt { get; set; }

    public MatchState Clone() => (MatchState)MemberwiseClone();
}
