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
        private const string DmcsCategory = "DMCS Compat";
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

        [ModOption("Lie Flat Chance", "Share of crippled NPCs that fall as a ragdoll and lie still; the others crawl with the game's own injured animations. Rolled once per NPC when its tendon is cut, so a change applies to the next cut.", nameof(LieFlatChanceValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        public static float LieFlatChance = OptionSpecs.LieFlatChance.Default;

        [ModOption("Kneel Chance", "Share of crawling NPCs that fall forward onto their knees and fight on kneeling upright; the others sit down and crawl on their back. Rolled once per NPC when its tendon is cut, so a change applies to the next cut.", nameof(KneelChanceValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(8)]
        [ModOptionSlider]
        public static float KneelChance = OptionSpecs.KneelChance.Default;

        [ModOption("Crawl Speed", "Movement speed while crawling, relative to walking.", nameof(CrawlSpeedValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(9)]
        [ModOptionSlider]
        public static float CrawlSpeed = OptionSpecs.CrawlSpeed.Default;

        [ModOption("Crawl Body Height", "How low the crawling body is held. 0 = flat on the ground, 1 = standing.", nameof(CrawlBodyHeightValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(10)]
        [ModOptionSlider]
        public static float CrawlBodyHeight = OptionSpecs.CrawlBodyHeight.Default;

        [ModOption("Steady Crawl Pose", "Small twitches of the crawling NPC count as standing still, so it does not flip between sitting and crawling (which looks like sitting in the air). Off: the game decides alone.")]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(11)]
        public static bool SteadyCrawlPose = true;

        [ModOption("Crawl Start Speed", "Steady Crawl Pose: how fast the NPC must move before it switches from sitting to crawling.", nameof(CrawlStartSpeedValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(12)]
        [ModOptionSlider]
        public static float CrawlStartSpeed = OptionSpecs.CrawlStartSpeed.Default;

        [ModOption("Crawl Stop Speed", "Steady Crawl Pose: below this speed a crawling NPC sits down again. Keep it below Crawl Start Speed.", nameof(CrawlStopSpeedValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(13)]
        [ModOptionSlider]
        public static float CrawlStopSpeed = OptionSpecs.CrawlStopSpeed.Default;

        [ModOption("Leg Strength", "How hard the legs of a crawling NPC follow its animation. x1 = the game's own value. Higher: it sits down faster after crawling instead of hanging on its shins.", nameof(LegStrengthValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(14)]
        [ModOptionSlider]
        public static float LegStrength = OptionSpecs.LegStrength.Default;

        [ModOption("Crawl Turn Speed", "How fast a crawling or kneeling NPC turns towards you, in percent of the game's own turn speed. 100 % = the game's speed: a half turn in about 0.2 s, as fast as a standing NPC.", nameof(CrawlTurnSpeedValues))]
        [ModOptionCategory(TendonCategory, 0)]
        [ModOptionOrder(15)]
        [ModOptionSlider]
        public static float CrawlTurnSpeed = OptionSpecs.CrawlTurnSpeed.Default;

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

        [ModOption("Weakening", "The NPC gets weaker as it bleeds: it crawls slower, screams less often and near the end only breathes heavily. Off: full strength until death.")]
        [ModOptionCategory(BleedCategory, 1)]
        [ModOptionOrder(4)]
        public static bool Weakening = true;

        [ModOption("Weakest Crawl Speed", "Crawl speed reached at the moment of death, in percent of Crawl Speed. 100 % = no slowdown.", nameof(WeakestCrawlSpeedValues))]
        [ModOptionCategory(BleedCategory, 1)]
        [ModOptionOrder(5)]
        [ModOptionSlider]
        public static float WeakestCrawlSpeed = OptionSpecs.WeakestCrawlSpeed.Default;

        [ModOption("Scream Slowdown", "How much longer the pause between screams is at the moment of death. x1 = no slowdown.", nameof(ScreamSlowdownValues))]
        [ModOptionCategory(BleedCategory, 1)]
        [ModOptionOrder(6)]
        [ModOptionSlider]
        public static float ScreamSlowdown = OptionSpecs.ScreamSlowdown.Default;

        [ModOption("Gasping From", "From this point of the bleed-out on, screams turn into heavy breathing. 100 % = never.", nameof(GaspingFromValues))]
        [ModOptionCategory(BleedCategory, 1)]
        [ModOptionOrder(7)]
        [ModOptionSlider]
        public static float GaspingFrom = OptionSpecs.GaspingFrom.Default;

        // ---- Ground Fighting (step 4, experimental) --------------------------

        [ModOption("Ground Fighting Enabled", "NPCs with a cut tendon fight on from the ground: screams, and crawling NPCs swing at you. Off: they only crawl or lie there.")]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(0)]
        public static bool GroundFightingEnabled = true;

        [ModOption("Ground Attacks", "A crawling NPC swings its weapon at you with its upper body when you are in reach (its own attack animations; needs a weapon). NPCs lying flat do not swing. A crawling NPC never uses its own melee AI, so with this off it does not attack.")]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(1)]
        public static bool GroundAttacks = true;

        [ModOption("Ground Attack Chance", "Share of crippled NPCs that fight from the ground; the others only crawl and scream. Rolled once per NPC when its tendon is cut, so a change applies to the next cut.", nameof(GroundAttackChanceValues))]
        [ModOptionCategory(GroundCategory, 2)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        public static float GroundAttackChance = OptionSpecs.GroundAttackChance.Default;

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

        // ---- DMCS Compat ----------------------------------------------------

        [ModOption("DMCS Adrenaline", "With DMCS installed: cutting a tendon and killing a crawling NPC with a cut tendon fill DMCS's Adrenaline meter. Without DMCS this does nothing.")]
        [ModOptionCategory(DmcsCategory, 3)]
        [ModOptionOrder(0)]
        public static bool DmcsAdrenaline = true;

        [ModOption("Heel Cut Adrenaline", "Adrenaline points for each heel cut that cripples an NPC (DMCS: a hit gives 5, a full meter is 100 by default).", nameof(HeelCutAdrenalineValues))]
        [ModOptionCategory(DmcsCategory, 3)]
        [ModOptionOrder(1)]
        [ModOptionSlider]
        public static float HeelCutAdrenaline = OptionSpecs.HeelCutAdrenaline.Default;

        [ModOption("Crippled Kill Adrenaline", "Adrenaline points when you kill an NPC that was already crawling or lying with a cut tendon (any of your hits; bleeding out gives nothing).", nameof(CrippledKillAdrenalineValues))]
        [ModOptionCategory(DmcsCategory, 3)]
        [ModOptionOrder(2)]
        [ModOptionSlider]
        public static float CrippledKillAdrenaline = OptionSpecs.CrippledKillAdrenaline.Default;

        // ---- Debug ----------------------------------------------------------

        [ModOption("Debug Logging", "Logs every player hit on an NPC with part, angle, damage type and verdict, plus collapse/release, bleed-out pulses and a one-line note for skipped hits, to the game log.")]
        [ModOptionCategory(DebugCategory, 4)]
        [ModOptionOrder(0)]
        public static bool DebugLogging = false;

        [ModOption("Show Heel Zones", "Draws the heel zone of every NPC as a sphere.")]
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
            };
        }

        // ---- Value sources (found by the game via valueSourceName) ----------

        public static ModOptionParameter[] HeelZoneSizeValues() => ToParameters(OptionSpecs.HeelZoneSize);

        public static ModOptionParameter[] HeelZoneBackOffsetValues() => ToParameters(OptionSpecs.HeelZoneBackOffset);

        public static ModOptionParameter[] HeelZoneHeightOffsetValues() => ToParameters(OptionSpecs.HeelZoneHeightOffset);

        public static ModOptionParameter[] MaxHitAngleValues() => ToParameters(OptionSpecs.MaxHitAngle);

        public static ModOptionParameter[] LieFlatChanceValues() => ToParameters(OptionSpecs.LieFlatChance);

        public static ModOptionParameter[] KneelChanceValues() => ToParameters(OptionSpecs.KneelChance);

        public static ModOptionParameter[] CrawlSpeedValues() => ToParameters(OptionSpecs.CrawlSpeed);

        public static ModOptionParameter[] CrawlBodyHeightValues() => ToParameters(OptionSpecs.CrawlBodyHeight);

        public static ModOptionParameter[] CrawlStartSpeedValues() => ToParameters(OptionSpecs.CrawlStartSpeed);

        public static ModOptionParameter[] CrawlStopSpeedValues() => ToParameters(OptionSpecs.CrawlStopSpeed);

        public static ModOptionParameter[] LegStrengthValues() => ToParameters(OptionSpecs.LegStrength);

        public static ModOptionParameter[] CrawlTurnSpeedValues() => ToParameters(OptionSpecs.CrawlTurnSpeed);

        public static ModOptionParameter[] BleedOutDurationValues() => ToParameters(OptionSpecs.BleedOutDuration);

        public static ModOptionParameter[] BloodPulseIntervalValues() => ToParameters(OptionSpecs.BleedPulseInterval);

        public static ModOptionParameter[] WeakestCrawlSpeedValues() => ToParameters(OptionSpecs.WeakestCrawlSpeed);

        public static ModOptionParameter[] ScreamSlowdownValues() => ToParameters(OptionSpecs.ScreamSlowdown);

        public static ModOptionParameter[] GaspingFromValues() => ToParameters(OptionSpecs.GaspingFrom);


        public static ModOptionParameter[] GroundAttackChanceValues() => ToParameters(OptionSpecs.GroundAttackChance);

        public static ModOptionParameter[] GroundAttackRangeValues() => ToParameters(OptionSpecs.GroundAttackRange);

        public static ModOptionParameter[] GroundAttackIntervalValues() => ToParameters(OptionSpecs.GroundAttackInterval);

        public static ModOptionParameter[] ScreamIntervalValues() => ToParameters(OptionSpecs.ScreamInterval);

        public static ModOptionParameter[] HeelCutAdrenalineValues() => ToParameters(OptionSpecs.HeelCutAdrenaline);

        public static ModOptionParameter[] CrippledKillAdrenalineValues() => ToParameters(OptionSpecs.CrippledKillAdrenaline);

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
