using EclipticaOverlay.Models;

namespace EclipticaOverlay.Services;

/// Reference lookup keyed by the boss name exactly as ECLIPTICA writes it to the log.
/// Source: https://ecliptica.miraheze.org/wiki (each boss's individual page).
///
/// Some bosses log a completely different name per phase instead of gating their phase-2+
/// moveset in place (e.g. "Despair" -> "DespairPhase2", "JimBringer" -> "JimBringerPhase2" ->
/// "JimBringerPhase3") — those get one BossInfo entry per phase name below, each carrying only
/// that phase's own strategy/affinity. Bosses that stay under one log name across phases (e.g.
/// "Steven", "JackedPumpkin") instead fold their later-phase behavior into a single entry's
/// Strategy/PhaseTrigger text.
public static class BossReference
{
    private static readonly Dictionary<string, BossInfo> Bosses = new()
    {
        // ---- Confirmed: log name seen directly in a log, or confirmed by a player ----

        ["Kakarot"] = new BossInfo(
            "Keeper of the Garden", "Physical", 1,
            "Alternates a forward slam with small AoE bursts; may throw rocks or leap at range."),

        ["DarkMouth"] = new BossInfo(
            "Eater of Fields", "Physical / Poison", 1,
            "Flying boss alternating poison projectile volleys with a high-damage charge.",
            WeakTo: "Shadow, Poison", ResistTo: "Luminous"),

        // Log name "FlyLord" corresponds to the wiki's "Beelzebub" (Lord of the Flies).
        ["FlyLord"] = new BossInfo(
            "Lord of the Flies", "Physical / Poison", 1,
            "Summons flies to help him automatically once he drops to 50% HP.",
            PhaseTrigger: "Auto-summons flies at 50% HP",
            WeakTo: "Fire, Shadow", ResistTo: "Frost, Luminous, Poison"),

        // Log name "QueenBug" corresponds to the wiki's "Vesra", queen of the Peltapod colony.
        ["QueenBug"] = new BossInfo(
            "Grand Matriarch of Peltapods", "Physical", 1,
            "At 50% HP she hides in an invulnerable cocoon and spawns a Peltapod Guard — kill it to force her out.",
            PhaseTrigger: "Cocoon phase at 50% HP",
            WeakTo: "Fire, Shadow", ResistTo: "Frost, Luminous, Poison"),

        ["Middleman"] = new BossInfo(
            "The Power of Megium", "Physical / Electric", 1,
            "Fast and close-range: charges, twin-sword slashes, aerial kicks and a piercing laser.",
            WeakTo: "Fire, Electric, Shadow", ResistTo: "Luminous, Poison"),

        ["Despair"] = new BossInfo(
            "Harbinger of the Hopeless", "Physical / Electric", 2,
            "Phase 1: lightning strikes and beams at range. Phase 2 hits far harder with bigger beams.",
            PhaseTrigger: "Phase 2 starts when phase 1 is defeated (not %-based)",
            WeakTo: "Fire, Frost, Luminous, Poison", ResistTo: "Electric, Shadow"),

        ["DespairPhase2"] = new BossInfo(
            "Harbinger of the Hopeless (Phase 2)", "Physical / Electric", 2,
            "Larger beams, more lightning, and a big AOE blast that can fire twice in a row.",
            WeakTo: "Fire, Frost, Luminous, Poison", ResistTo: "Electric, Shadow"),

        ["Steven"] = new BossInfo(
            "Protector of the Pancakes", "Physical / Fire", 2,
            "At 50% HP he goes enflamed — adds fire damage/burn and throws far more syrup puddles.",
            PhaseTrigger: "Enflamed phase at 50% HP",
            WeakTo: "Frost, Shadow", ResistTo: "Fire, Luminous"),

        ["BlackLily"] = new BossInfo(
            "A Swarming Illuminate", "Luminous / Shadow", 1,
            "Stationary; gains much harder, barely-telegraphed moves once it drops below 50% HP.",
            PhaseTrigger: "New moves at 50% HP",
            WeakTo: "Fire, Frost", ResistTo: "Luminous, Shadow"),

        // Log name "Melon" corresponds to the wiki's "Melgor Johnson" (his own lore literally
        // says "My name'a Melon"). Logs "MelonPhase2" separately for phase 2.
        ["Melon"] = new BossInfo(
            "The Fallen Employee", "Physical / Electric", 2,
            "Standard physical/electric combo attacks up close; phase 2 significantly powers this moveset up.",
            WeakTo: "Frost, Shadow", ResistTo: "Electric"),

        ["MelonPhase2"] = new BossInfo(
            "The Fallen Employee (Phase 2)", "Physical / Electric", 2,
            "Enhanced moveset with extra damage and more electric attacks; may clap out a massive purple shockwave at any health.",
            WeakTo: "Frost, Shadow", ResistTo: "Electric"),

        // Log name confirmed as "M41D" (wiki: "M-41-D"). Phase 2 (tower summon) gates in place —
        // no separate "M41DPhase2" log name has been observed.
        ["M41D"] = new BossInfo(
            "The Servant Alloy", "Electric", 2,
            "At 40% HP she summons 2 Disruption Towers that alternate healing her or firing an electric AoE — kill them fast.",
            PhaseTrigger: "Tower phase at 40% HP",
            WeakTo: "Electric, Shadow", ResistTo: "Fire, Luminous, Poison"),

        // Logs "YukiPhase2" separately for the flying phase.
        ["Yuki"] = new BossInfo(
            "Ice-born Android", "Physical / Frost", 2,
            "Punches and grapples up close; at 50% HP gains a ground-pound that emits purple shockwaves.",
            PhaseTrigger: "Ground-pound shockwaves unlock at 50% HP",
            WeakTo: "Electric"),

        ["YukiPhase2"] = new BossInfo(
            "Ice-born Android (Phase 2)", "Physical / Frost", 2,
            "Permanent flight with ranged ice attacks; at 50% HP unlocks the full moveset — ice-wall arena splits and lasers.",
            PhaseTrigger: "Full moveset (ice walls + lasers) at 50% HP",
            WeakTo: "Electric"),

        // Log name confirmed as "AntKing" (wiki page title "Khepri", queen Vesra's successor).
        // Logs "AntKingPhase2" separately for the flight phase.
        ["AntKing"] = new BossInfo(
            "The Ant King", "Physical / Frost", 2,
            "Chainable ice 'pizza cutter' swipes (up to 3 in a row); at range, throws an always-accurate freezing ice spike.",
            WeakTo: "Fire, Shadow", ResistTo: "Frost, Luminous"),

        ["AntKingPhase2"] = new BossInfo(
            "The Ant King (Phase 2)", "Physical / Frost", 2,
            "Permanent flight and much higher mobility. May scream to freeze nearby players before going airborne, then grabs players or fires a shredding ice-projectile spread that inflicts a long freeze.",
            WeakTo: "Fire, Shadow", ResistTo: "Frost, Luminous"),

        // Eye of the Eclipse final boss — log name confirmed as "JimBringer", with separate
        // "JimBringerPhase2" (the Flipper vehicle) and "JimBringerPhase3" log names per phase.
        // All of his direct melee attacks deal unresistable "Purple" damage regardless of phase.
        // Aggro is stated to be entirely random in every phase, per the wiki. Only fightable if
        // his Associated Artifact (HC Armor Plating) was the most-sacrificed one at the
        // Dimensional Gate. Phase 1/2 are neutral to every element; phase 3 gains 0.75x
        // resistance to everything but Physical — since that differs per phase, WeakTo/ResistTo
        // are set per entry below rather than shared.
        ["JimBringer"] = new BossInfo(
            "The Man of Many Titles", "Physical", 3,
            "All direct melee hits deal unresistable Purple damage. Random-aggro melee boss: 70% HP unlocks a leap and his laser; 40% HP unlocks 'Cataclysm', a near-arena-wide nuke (center = instant death, falls off with distance) followed by a long passive window. Also summons a Jim's Wedge add at 70/50/30% HP.",
            PhaseTrigger: "Cataclysm unlocks at 40% HP"),

        ["JimBringerPhase2"] = new BossInfo(
            "The Man of Many Titles (Phase 2 — Flipper MK.II Abyssal)", "Physical", 3,
            "Jim rides the Flipper, which is fully invincible (all damage is Blocked) for the whole phase. It rams for heavy Purple damage, deploys a Jim's Wedge, or barrages rockets at range. After 2 minutes, 4 Signal Towers drop in the arena's corners — standing in one charges it (30s solo, faster with more players in the same tower); once all 4 are charged, an orbital strike destroys the Flipper.",
            PhaseTrigger: "Signal Towers drop 2 minutes into the phase"),

        ["JimBringerPhase3"] = new BossInfo(
            "The Man of Many Titles (Phase 3)", "Physical", 3,
            "All direct melee hits deal unresistable Purple damage. Phase 1's moveset, powered up — faster swings, longer combos, stronger enders, plus teleports (to distant players or mid-combo). 70% HP re-unlocks a stronger leap and a wider/faster laser. 50% HP starts a sub-phase: a Product Barrel drops opposite Jim, who becomes invincible and jogs toward it — destroy it before he reaches it, or he heals a large chunk and gains a long Empowered+Enforced buff (can't drop another barrel until back below 50%). This sub-phase also unlocks arena-wide moves: Nutnado (tornado, pulls players airborne), Nutdust (gravity slam that only hits airborne players, inflicts Heavy), and Tableflip (directional arena-wide slam). 40% HP regains Cataclysm.",
            PhaseTrigger: "Product Barrel sub-phase at 50% HP; Cataclysm returns at 40% HP",
            ResistTo: "Fire, Frost, Electric, Luminous, Shadow, Poison"),

        // Log name "JackedPumpkin" corresponds to the wiki's "Jacked O' Lantern".
        ["JackedPumpkin"] = new BossInfo(
            "Bad Things, Good People", "Physical / Fire", 2,
            "Phase 2 heals a big chunk and adds fire-enhanced swings and pillars, plus a self-buff.",
            PhaseTrigger: "Phase 2 at 35% HP",
            WeakTo: "Fire, Shadow, Poison", ResistTo: "Frost, Electric, Luminous"),

        // Log name confirmed as "Oone" (no hyphen; wiki title "O-One").
        ["Oone"] = new BossInfo(
            "The First Opton", "Physical", 2,
            "Ranged axe+pistol phase; switches to a single sword for faster close-range attacks at 40% HP.",
            PhaseTrigger: "Sword phase at 40% HP",
            WeakTo: "Frost, Shadow", ResistTo: "Luminous"),

        // Log-confirmed (matches the prior best-guess key exactly).
        ["Maxipuss"] = new BossInfo(
            "The Rodent Hunter", "Physical", 1,
            "Melee punches and grapples up close; throws hairballs or leaps in from range.",
            WeakTo: "Frost, Shadow", ResistTo: "Luminous"),

        // Log name confirmed as "Nan" (not "NaN" — different casing than previously guessed).
        ["Nan"] = new BossInfo(
            "An Elemental Machine", "Fire / Frost / Electric / Luminous / Shadow", 1,
            "Cycles between 5 elemental modes; only vulnerable to whichever element it's currently using."),

        // Log-confirmed (matches the prior best-guess key exactly).
        ["Pride"] = new BossInfo(
            "Prideful of Power", "Physical / Luminous", 1,
            "Non-damaging heart projectiles and beams; periodically flies up and rains hearts from above.",
            WeakTo: "Electric", ResistTo: "Luminous, Shadow"),

        // Log-confirmed (matches the prior best-guess key exactly).
        ["Kodama"] = new BossInfo(
            "A Curse Incarnate", "Shadow", 1,
            "Stationary; a damaging shadow ring circles the arena alongside AOE blasts and tentacle patterns.",
            WeakTo: "Luminous", ResistTo: "Physical"),

        // Log-confirmed (matches the prior best-guess key exactly).
        ["Gravetender"] = new BossInfo(
            "The Aperture's Demon", "Physical / Shadow", 1,
            "Semi-stationary, burrows to reposition; spawns an unkillable Gravity Well that pulls players in.",
            WeakTo: "Frost, Luminous", ResistTo: "Fire"),

        // Log name confirmed as "NX-Obsidian" (with hyphen — previously guessed without one).
        ["NX-Obsidian"] = new BossInfo(
            "STATUS SET: DESTROY", "Physical / Fire", 1,
            "Unusually weak to Physical; has an unparryable purple explosion and burn-heavy lasers.",
            WeakTo: "Physical, Shadow", ResistTo: "Fire, Frost, Electric, Luminous"),

        // Log-confirmed (matches the prior best-guess key exactly).
        ["Amaziah"] = new BossInfo(
            "KILL YOU KILL YOU KILL YOU", "Physical / Poison", 1,
            "Close-range poison-projectile attacks; gains bigger jumps, lunges and face-beams at 60% HP.",
            PhaseTrigger: "New attacks at 60% HP",
            WeakTo: "Frost, Shadow", ResistTo: "Fire, Luminous, Poison"),

        // Log-confirmed (matches the prior best-guess key exactly). Logs "CorusPhase2" separately.
        ["Corus"] = new BossInfo(
            "The First Chorus", "Fire", 2,
            "Constant fireball barrages spread across the battlefield; jump attack throws a bigger fireball that leaves a lingering puddle.",
            PhaseTrigger: "Death of phase 1 triggers phase 2 (not %-based)",
            WeakTo: "Frost, Shadow", ResistTo: "Fire, Luminous"),

        ["CorusPhase2"] = new BossInfo(
            "The First Chorus (Phase 2)", "Fire", 2,
            "Most fireballs now leave a puddle of fire on the ground, and the jump attack leaves a growing fire pillar — the arena becomes largely untraversable up close.",
            WeakTo: "Frost, Shadow", ResistTo: "Fire, Luminous"),

        // Log-confirmed (matches the prior best-guess key exactly).
        ["GoldenGrouch"] = new BossInfo(
            "The Forever Trapped", "Physical / Shadow", 2,
            "Sword phase vs. barehanded phase; barehanded phase summons decaying adds that grant brief invincibility.",
            PhaseTrigger: "Disarmed below 70% HP",
            WeakTo: "Fire, Luminous", ResistTo: "Frost, Electric, Poison"),

        // ---- Unconfirmed: not yet seen in a log or confirmed by a player. Keyed by a best
        // guess at the internal name (wiki title with spaces/punctuation stripped, matching the
        // pattern of the confirmed bosses above). If a key is wrong here, the boss simply won't
        // show a reference panel — it won't show wrong info. Report corrections as you find them. ----

        ["BuffNoob"] = new BossInfo(
            "Can You Lift?", "Physical", 1,
            "First purple-attack boss; eats Pizza/Cola to heal and buff itself, roughly twice around 40% HP.",
            PhaseTrigger: "Self-buffs around 40% HP",
            WeakTo: "Electric, Shadow", ResistTo: "Luminous"),

        ["ConeHead"] = new BossInfo(
            "The Urban Samurai", "Physical", 2,
            "Traffic-light gimmick — move on green, stop on red. Phase 2 adds 2 more color-coded swords.",
            PhaseTrigger: "Disarmed (melee sub-phase) at 30% HP",
            WeakTo: "Shadow", ResistTo: "Luminous"),

        ["Mephiel"] = new BossInfo(
            "The Treacherous", "Shadow", 2,
            "Bullet-hell arena hazards; phase 2 grants flight and a spinning 'pizza cutter' sweep.",
            WeakTo: "Luminous", ResistTo: "Electric"),

        ["Irides"] = new BossInfo(
            "The Crystalline Prototype", "Physical / Electric / Luminous / Shadow", 2,
            "Phase 2 adds lightning/light attacks; below 50% HP summons 3 crystals that make it invincible — only their target can damage them.",
            PhaseTrigger: "Invincible crystals summoned below 50% HP",
            WeakTo: "Physical, Frost", ResistTo: "Fire, Luminous"),

        ["Abaddon"] = new BossInfo(
            "Demon of Destruction", "Physical / Shadow", 2,
            "Summons Elder Manalyte adds (3, then 6 in phase 2); AOE fire pillars and decay orbs, phase-2 attacks pierce defense.",
            WeakTo: "Luminous", ResistTo: "Fire, Frost, Electric, Poison"),

        ["Pandora"] = new BossInfo(
            "The Arrogance Safeguard", "Physical / Frost", 2,
            "Flying boss with ice AOEs and dashes; becomes far more aggressive with bigger piercing ice blasts below 50% HP.",
            PhaseTrigger: "More aggressive below 50% HP",
            WeakTo: "Fire, Luminous", ResistTo: "Frost, Electric, Shadow"),

        ["Bravera"] = new BossInfo(
            "The Opton Commander", "Physical / Electric", 2,
            "Spawns 2 guards that harass the lowest-HP player; unlocks a leap and a 'pizza cutter' at 50% HP each phase.",
            PhaseTrigger: "New move at 50% HP each phase",
            WeakTo: "Fire, Poison", ResistTo: "Frost, Electric, Luminous"),

        ["NeoPilot"] = new BossInfo(
            "Empress of FOX", "Physical / Electric", 2,
            "Kicks airborne players; sub-phase 2 heals and adds electric leap shockwaves, a tankbuster and a triple 'pizza cutter'.",
            PhaseTrigger: "Phase 2 at 40% HP",
            WeakTo: "Fire, Shadow", ResistTo: "Electric, Luminous"),

    };

    public static bool TryGet(string? bossName, out BossInfo info)
    {
        if (bossName != null && Bosses.TryGetValue(bossName, out var found))
        {
            info = found;
            return true;
        }

        info = null!;
        return false;
    }
}
