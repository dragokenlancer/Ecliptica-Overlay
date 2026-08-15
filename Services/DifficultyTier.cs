namespace EclipticaOverlay.Services;

/// Names the world difficulty tier (Prime/Penumbra/Antumbra/Umbra/Eclipse) a given run-progress
/// "phase" float falls into. The wiki's boss list gates bosses by these named tiers, but only
/// gives qualitative "Late X / Early Y" ranges, not exact numbers — so the boundaries below are
/// estimated from where known bosses were actually observed to appear in real logs:
///   - Prime/Penumbra   (~0.17): Vesra "QueenBug" (Late Prime/Early Penumbra) seen at phase 0.185
///   - Penumbra/Antumbra (~0.37): The Black Lily and Middleman (both Late Penumbra/Early Antumbra)
///     seen at 0.329 and 0.415
///   - Antumbra/Umbra   (~0.52): Despair (Late Antumbra/Early Umbra) seen at phase 0.507
/// Umbra/Eclipse and the Eclipse ceiling are extrapolated from the same ~0.15-0.19 tier width
/// pattern — no log data reaches that far yet, so treat those two as rough guesses.
public static class DifficultyTier
{
    private static readonly (double UpperBound, string Name)[] Tiers =
    {
        (0.17, "Prime"),
        (0.37, "Penumbra"),
        (0.52, "Antumbra"),
        (0.68, "Umbra"),       
        (double.MaxValue, "Eclipse"), 
    };

    public static string Name(double phase)
    {
        foreach (var (upperBound, name) in Tiers)
        {
            if (phase < upperBound)
                return name;
        }

        return "Eclipse";
    }
}
