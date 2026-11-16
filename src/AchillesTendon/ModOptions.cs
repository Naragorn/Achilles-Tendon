using AchillesTendon.Logic;
using ThunderRoad;

namespace AchillesTendon
{
    /// <summary>
    /// Every setting of the mod, shown in the game's Mod Menu.
    ///
    /// How the game handles these (read from ThunderRoad.dll 1.3.2, see README):
    /// - ModManager scans all static members with [ModOption] in this assembly.
    /// - The initial field value is the default; for floats the value list comes
    ///   from <see cref="OptionSpecs"/> so the default matches exactly.
    /// - A change in the menu writes the static field immediately (live), and the
    ///   chosen index is saved by the game per mod folder (persistent).
    /// - The game's mod page has its own "Reset Settings" button that resets every
    ///   option of this mod, so the mod does not add its own reset option.
    /// </summary>
    public static class ModOptions
    {
        private const string TendonCategory = "Achilles Tendon";
        private const string BleedCategory = "Bleed-Out";
        private const string GroundCategory = "Ground Fighting";
        private const string PlayerCategory = "Player Hardcore Mode";
        private const string DebugCategory = "Debug";

        // ---- Achilles Tendon ------------------------------------------------

        // The game saves options by name per mod, so each feature's on/off toggle
        // needs a unique name; a plain "Enabled" in every category would collide.
        [ModOption("Achilles Tendon Enabled", "Turns the mod on or off: heel hit detection and the collapse of NPCs with a cut tendon.")]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(0)]
        public static bool TendonEnabled = true;

        [ModOption("Heel Zone Size", "Radius of the heel zone around the Achilles tendon.", nameof(HeelZoneSizeValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        public static float HeelZoneSize = OptionSpecs.HeelZoneSize.Default;

        [ModOption("Heel Zone Back Offset", "How far behind the ankle the heel zone sits.", nameof(HeelZoneBackOffsetValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        public static float HeelZoneBackOffset = OptionSpecs.HeelZoneBackOffset.Default;

        [ModOption("Heel Zone Height Offset", "How far above the ankle the heel zone sits.", nameof(HeelZoneHeightOffsetValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        public static float HeelZoneHeightOffset = OptionSpecs.HeelZoneHeightOffset.Default;

        [ModOption("Max Hit Angle", "How far from straight behind you may stand. 0 = only directly behind, 90 = side, 180 = anywhere.", nameof(MaxHitAngleValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        public static float MaxHitAngle = OptionSpecs.MaxHitAngle.Default;

        [ModOption("Allowed Damage Types", "Which damage types can cut the tendon. Blunt never does.")]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(5)]
        public static AllowedDamageTypes AllowedDamageTypes = AllowedDamageTypes.SlashAndPierce;

        [ModOption("Stand Up When Disabled", "When Achilles Tendon is turned off, NPCs with a cut tendon get up again. Off: they stay down.")]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(6)]
        public static bool StandUpWhenDisabled = true;

        [ModOption("Downed Pose", "Crawl: the NPC drops into the game's own injured pose and crawls when it moves. Lie Flat: it falls as a ragdoll and lies still.")]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(7)]
        public static DownedPose DownedPose = DownedPose.Crawl;

        [ModOption("Crawl Speed", "Movement speed while crawling, relative to walking.", nameof(CrawlSpeedValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(8)]
        [ModOptionSlider]
        public static float CrawlSpeed = OptionSpecs.CrawlSpeed.Default;

        [ModOption("Crawl Body Height", "How low the crawling body is held. 0 = flat on the ground, 1 = standing.", nameof(CrawlBodyHeightValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(9)]
        [ModOptionSlider]
        public static float CrawlBodyHeight = OptionSpecs.CrawlBodyHeight.Default;

        // ---- Bleed-Out ------------------------------------------------------

        [ModOption("Bleed-Out Enabled", "NPCs with a cut tendon bleed out and die after Bleed-Out Duration. Off: the bleeding pauses (it goes on when turned back on).")]
        [ModOptionCategory(BleedCategory, 1)]
        [ModOptionOrder(0)]
        public static bool BleedOutEnabled = true;

        [ModOption("Bleed-Out Duration", "Seconds from the cut to death. Health drains evenly; hitting the NPC still kills it sooner.", nameof(BleedOutDurationValues))]
        [ModOptionCategory(BleedCategory, 1)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        public static float BleedOutDuration = OptionSpecs.BleedOutDuration.Default;

        [ModOption("Blood Effects", "Blood drips from the cut heel, growing stronger towards the end. The game's own blood setting still applies.")]
        [ModOptionCategory(BleedCategory, 1)]
        [ModOptionOrder(2)]
        public static bool BloodEffects = true;

        [ModOption("Blood Pulse Interval", "Seconds between blood drips (and bleed log lines with Debug Logging).", nameof(BloodPulseIntervalValues))]
        [ModOptionCategory(BleedCategory, 1)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        public static float BloodPulseInterval = OptionSpecs.BleedPulseInterval.Default;

        // ---- Ground Fighting (step 4, experimental) --------------------------

        [ModOption("Ground Fighting Enabled", "NPCs with a cut tendon fight on from the ground: screams, and (experimental) stronger arms and swings. Off: they just lie there.")]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(0)]
        public static bool GroundFightingEnabled = true;

        [ModOption("Arm Strength", "How hard the arms of a lying NPC follow its animation. x1 = the game's own value for a lying body (limp, default). Higher values stiffen the arms.", nameof(ArmStrengthValues))]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        public static float ArmStrength = OptionSpecs.ArmStrength.Default;

        [ModOption("Ground Attacks", "A downed NPC swings its weapon at you when you are in reach (its own attack animations; needs a weapon). Crawling: upper body only, its own melee AI is off. Lying flat: experimental, no visible swing yet.")]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(2)]
        public static bool GroundAttacks = true;

        [ModOption("Ground Attack Range", "How close you must be for a lying NPC to swing at you.", nameof(GroundAttackRangeValues))]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        public static float GroundAttackRange = OptionSpecs.GroundAttackRange.Default;

        [ModOption("Ground Attack Interval", "Seconds between two swings from the ground.", nameof(GroundAttackIntervalValues))]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(4)]
        [ModOptionSlider]
        public static float GroundAttackInterval = OptionSpecs.GroundAttackInterval.Default;

        [ModOption("Screaming", "A lying NPC cries out in pain now and then.")]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(5)]
        public static bool Screaming = true;

        [ModOption("Scream Interval", "Seconds between two screams.", nameof(ScreamIntervalValues))]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        public static float ScreamInterval = OptionSpecs.ScreamInterval.Default;

        // ---- Player Hardcore Mode -------------------------------------------

        [ModOption("Apply To Player", "Hardcore: NPC hits on your own heel count too, with the same rules (zone, angle, damage type). Off by default.")]
        [ModOptionCategory(PlayerCategory, 3)]
        [ModOptionOrder(0)]
        public static bool ApplyToPlayer = false;

        // ---- Debug ----------------------------------------------------------

        [ModOption("Debug Logging", "Logs every player hit on an NPC (and, with Apply To Player, NPC hits on you) with part, angle, damage type and verdict, plus collapse/release, bleed-out pulses and a one-line note for skipped hits, to the game log.")]
        [ModOptionCategory(DebugCategory, 4)]
        [ModOptionOrder(0)]
        public static bool DebugLogging = false;

        [ModOption("Show Heel Zones", "Draws the heel zone of every NPC (and yours, with Apply To Player) as a sphere.")]
        [ModOptionCategory(DebugCategory, 4)]
        [ModOptionOrder(1)]
        public static bool ShowHeelZones = false;

        [ModOption("Show Hit Markers", "Marks each leg hit: green = heel hit, red = miss.")]
        [ModOptionCategory(DebugCategory, 4)]
        [ModOptionOrder(2)]
        public static bool ShowHitMarkers = false;

        [ModOption("Hit Marker Duration", "How long a hit marker stays visible.", nameof(HitMarkerDurationValues))]
        [ModOptionCategory(DebugCategory, 4)]
        [ModOptionOrder(3)]
        [ModOptionSlider]
        public static float HitMarkerDuration = OptionSpecs.HitMarkerDuration.Default;

        public static HeelHitSettings CurrentHeelHitSettings()
        {
            return new HeelHitSettings
            {
                ZoneRadius = HeelZoneSize,
                ZoneBackOffset = HeelZoneBackOffset,
                ZoneHeightOffset = HeelZoneHeightOffset,
                MaxHitAngle = MaxHitAngle,
                AllowedDamageTypes = AllowedDamageTypes,
                ApplyToPlayer = ApplyToPlayer,
            };
        }

        // ---- Value sources (found by the game via valueSourceName) ----------

        public static ModOptionParameter[] HeelZoneSizeValues() => ToParameters(OptionSpecs.HeelZoneSize);

        public static ModOptionParameter[] HeelZoneBackOffsetValues() => ToParameters(OptionSpecs.HeelZoneBackOffset);

        public static ModOptionParameter[] HeelZoneHeightOffsetValues() => ToParameters(OptionSpecs.HeelZoneHeightOffset);

        public static ModOptionParameter[] MaxHitAngleValues() => ToParameters(OptionSpecs.MaxHitAngle);

        public static ModOptionParameter[] CrawlSpeedValues() => ToParameters(OptionSpecs.CrawlSpeed);

        public static ModOptionParameter[] CrawlBodyHeightValues() => ToParameters(OptionSpecs.CrawlBodyHeight);

        public static ModOptionParameter[] BleedOutDurationValues() => ToParameters(OptionSpecs.BleedOutDuration);

        public static ModOptionParameter[] BloodPulseIntervalValues() => ToParameters(OptionSpecs.BleedPulseInterval);

        public static ModOptionParameter[] ArmStrengthValues() => ToParameters(OptionSpecs.ArmStrength);

        public static ModOptionParameter[] GroundAttackRangeValues() => ToParameters(OptionSpecs.GroundAttackRange);

        public static ModOptionParameter[] GroundAttackIntervalValues() => ToParameters(OptionSpecs.GroundAttackInterval);

        public static ModOptionParameter[] ScreamIntervalValues() => ToParameters(OptionSpecs.ScreamInterval);

        public static ModOptionParameter[] HitMarkerDurationValues() => ToParameters(OptionSpecs.HitMarkerDuration);

        private static ModOptionParameter[] ToParameters(FloatOptionSpec spec)
        {
            var values = spec.Values();
            var parameters = new ModOptionParameter[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                parameters[i] = new ModOptionFloat(spec.Label(values[i]), values[i]);
            }
            return parameters;
        }
    }
}
