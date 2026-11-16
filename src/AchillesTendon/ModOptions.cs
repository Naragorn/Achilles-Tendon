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

        // ---- Player Hardcore Mode -------------------------------------------

        [ModOption("Apply To Player", "Hardcore: NPC hits on your own heel count too, with the same rules (zone, angle, damage type). Off by default.")]
        [ModOptionCategory(PlayerCategory, 2)]
        [ModOptionOrder(0)]
        public static bool ApplyToPlayer = false;

        // ---- Debug ----------------------------------------------------------

        [ModOption("Debug Logging", "Logs every player hit on an NPC (and, with Apply To Player, NPC hits on you) with part, angle, damage type and verdict, plus collapse/release, bleed-out pulses and a one-line note for skipped hits, to the game log.")]
        [ModOptionCategory(DebugCategory, 3)]
        [ModOptionOrder(0)]
        public static bool DebugLogging = false;

        [ModOption("Show Heel Zones", "Draws the heel zone of every NPC (and yours, with Apply To Player) as a sphere.")]
        [ModOptionCategory(DebugCategory, 3)]
        [ModOptionOrder(1)]
        public static bool ShowHeelZones = false;

        [ModOption("Show Hit Markers", "Marks each leg hit: green = heel hit, red = miss.")]
        [ModOptionCategory(DebugCategory, 3)]
        [ModOptionOrder(2)]
        public static bool ShowHitMarkers = false;

        [ModOption("Hit Marker Duration", "How long a hit marker stays visible.", nameof(HitMarkerDurationValues))]
        [ModOptionCategory(DebugCategory, 3)]
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

        public static ModOptionParameter[] BleedOutDurationValues() => ToParameters(OptionSpecs.BleedOutDuration);

        public static ModOptionParameter[] BloodPulseIntervalValues() => ToParameters(OptionSpecs.BleedPulseInterval);

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
