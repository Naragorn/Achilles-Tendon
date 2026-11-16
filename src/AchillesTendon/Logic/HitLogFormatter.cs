using System.Globalization;

namespace AchillesTendon.Logic
{
    /// <summary>Everything one debug log line about a leg hit shows.</summary>
    public sealed class HitLogEntry
    {
        public string Target;
        public string PartName;
        public string PartType;
        public string PartSection;
        public string DamageType;
        public float Damage;

        /// <summary>
        /// Angle between the game's impactVelocity and the target's facing, logged
        /// for comparison only; its sign convention is not verified (see README).
        /// </summary>
        public float ImpactVelocityAngle = float.NaN;

        public HeelHitResult Result;
        public float ZoneRadius;
        public float MaxHitAngle;
    }

    public static class HitLogFormatter
    {
        private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

        public static string Format(HitLogEntry e)
        {
            var verdict = e.Result.IsHeelHit ? "HEEL HIT" : "miss (" + e.Result.Rejection + ")";
            var distance = float.IsNaN(e.Result.DistanceToZoneCenter)
                ? "n/a"
                : e.Result.DistanceToZoneCenter.ToString("0.000", Culture) + "m";

            return verdict
                + " | target=" + e.Target
                + " | part=" + e.PartName + " type=" + e.PartType + " section=" + e.PartSection
                + " | damage=" + e.DamageType + " " + e.Damage.ToString("0.00", Culture)
                + " | zone dist=" + distance + " r=" + e.ZoneRadius.ToString("0.000", Culture) + "m"
                + " | angleFromBehind=" + Degrees(e.Result.AttackAngle)
                + " max=" + e.MaxHitAngle.ToString("0", Culture) + "°"
                + " | impactVelAngle=" + Degrees(e.ImpactVelocityAngle);
        }

        private static string Degrees(float value)
        {
            return float.IsNaN(value) ? "n/a" : value.ToString("0.0", Culture) + "°";
        }
    }
}
