using System;
using System.Collections.Generic;
using System.Globalization;

namespace AchillesTendon.Logic
{
    public enum OptionUnit
    {
        /// <summary>Stored in meters, shown in centimeters.</summary>
        Centimeters,
        Degrees,
        Seconds,
        Meters,
        Multiplier,

        /// <summary>Stored as 0..1, shown in percent.</summary>
        Percent,

        /// <summary>A bare number (no single unit fits, e.g. m/s plus degrees/s).</summary>
        Plain,

        /// <summary>Whole points (e.g. DMCS adrenaline).</summary>
        Points,
    }

    /// <summary>
    /// Range, step and default of a numeric Mod Menu option. Values are generated
    /// with decimal arithmetic so the default is bit-identical to one entry: the
    /// game's mod menu looks the default up with object.Equals on the boxed float.
    /// </summary>
    public sealed class FloatOptionSpec
    {
        public FloatOptionSpec(string name, float min, float max, float step, float defaultValue, OptionUnit unit)
        {
            Name = name;
            Min = min;
            Max = max;
            Step = step;
            Default = defaultValue;
            Unit = unit;
        }

        public string Name { get; }
        public float Min { get; }
        public float Max { get; }
        public float Step { get; }
        public float Default { get; }
        public OptionUnit Unit { get; }

        public float[] Values()
        {
            var min = (decimal)Min;
            var step = (decimal)Step;
            var count = (int)Math.Round(((decimal)Max - min) / step) + 1;
            var values = new float[count];
            for (var i = 0; i < count; i++)
            {
                values[i] = (float)(min + i * step);
            }
            return values;
        }

        public string Label(float value)
        {
            var culture = CultureInfo.InvariantCulture;
            switch (Unit)
            {
                case OptionUnit.Centimeters:
                    return ((decimal)value * 100m).ToString("0.#", culture) + " cm";
                case OptionUnit.Degrees:
                    return ((decimal)value).ToString("0.#", culture) + "°";
                case OptionUnit.Meters:
                    return ((decimal)value).ToString("0.0#", culture) + " m";
                case OptionUnit.Multiplier:
                    return "x" + ((decimal)value).ToString("0.0#", culture);
                case OptionUnit.Percent:
                    return ((decimal)value * 100m).ToString("0.#", culture) + " %";
                case OptionUnit.Plain:
                    return ((decimal)value).ToString("0.0#", culture);
                case OptionUnit.Points:
                    return ((decimal)value).ToString("0", culture);
                default:
                    return ((decimal)value).ToString("0.#", culture) + " s";
            }
        }
    }

    /// <summary>Every numeric Mod Menu option of the mod. No magic numbers elsewhere.</summary>
    public static class OptionSpecs
    {
        public static readonly FloatOptionSpec HeelZoneSize =
            new FloatOptionSpec("Heel Zone Size", 0.02f, 0.2f, 0.01f, 0.08f, OptionUnit.Centimeters);

        public static readonly FloatOptionSpec HeelZoneBackOffset =
            new FloatOptionSpec("Heel Zone Back Offset", 0f, 0.15f, 0.01f, 0.04f, OptionUnit.Centimeters);

        public static readonly FloatOptionSpec HeelZoneHeightOffset =
            new FloatOptionSpec("Heel Zone Height Offset", -0.05f, 0.15f, 0.01f, 0.03f, OptionUnit.Centimeters);

        public static readonly FloatOptionSpec MaxHitAngle =
            new FloatOptionSpec("Max Hit Angle", 30f, 180f, 5f, 110f, OptionUnit.Degrees);

        public static readonly FloatOptionSpec CrawlSpeed =
            new FloatOptionSpec("Crawl Speed", 0.05f, 1f, 0.05f, 0.3f, OptionUnit.Multiplier);

        public static readonly FloatOptionSpec CrawlBodyHeight =
            new FloatOptionSpec("Crawl Body Height", 0f, 0.6f, 0.05f, 0.2f, OptionUnit.Multiplier);

        // In the animator's Speed/Strafe units (local velocity, about m/s).
        public static readonly FloatOptionSpec CrawlStartSpeed =
            new FloatOptionSpec("Crawl Start Speed", 0.1f, 1f, 0.05f, 0.4f, OptionUnit.Plain);

        public static readonly FloatOptionSpec CrawlStopSpeed =
            new FloatOptionSpec("Crawl Stop Speed", 0.05f, 0.5f, 0.05f, 0.1f, OptionUnit.Plain);

        public static readonly FloatOptionSpec LegStrength =
            new FloatOptionSpec("Leg Strength", 1f, 10f, 0.5f, 4f, OptionUnit.Multiplier);

        public static readonly FloatOptionSpec BleedOutDuration =
            new FloatOptionSpec("Bleed-Out Duration", 10f, 180f, 5f, 45f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec BleedPulseInterval =
            new FloatOptionSpec("Blood Pulse Interval", 0.5f, 10f, 0.5f, 2f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec ArmStrength =
            new FloatOptionSpec("Arm Strength", 1f, 10f, 0.5f, 1f, OptionUnit.Multiplier);

        public static readonly FloatOptionSpec GroundAttackChance =
            new FloatOptionSpec("Ground Attack Chance", 0f, 1f, 0.05f, 0.75f, OptionUnit.Percent);

        public static readonly FloatOptionSpec GroundAttackRange =
            new FloatOptionSpec("Ground Attack Range", 0.5f, 4f, 0.1f, 1.8f, OptionUnit.Meters);

        public static readonly FloatOptionSpec GroundAttackInterval =
            new FloatOptionSpec("Ground Attack Interval", 1f, 10f, 0.5f, 3f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec ScreamInterval =
            new FloatOptionSpec("Scream Interval", 2f, 30f, 1f, 6f, OptionUnit.Seconds);

        public static readonly FloatOptionSpec WeakestCrawlSpeed =
            new FloatOptionSpec("Weakest Crawl Speed", 0.05f, 1f, 0.05f, 0.25f, OptionUnit.Percent);

        public static readonly FloatOptionSpec ScreamSlowdown =
            new FloatOptionSpec("Scream Slowdown", 1f, 5f, 0.5f, 3f, OptionUnit.Multiplier);

        public static readonly FloatOptionSpec GaspingFrom =
            new FloatOptionSpec("Gasping From", 0.5f, 1f, 0.05f, 0.75f, OptionUnit.Percent);

        public static readonly FloatOptionSpec HitMarkerDuration =
            new FloatOptionSpec("Hit Marker Duration", 1f, 30f, 1f, 5f, OptionUnit.Seconds);

        // DMCS's own gains for scale: hit 5, disarm 15, charge stagger 15, meter 100.
        public static readonly FloatOptionSpec HeelCutAdrenaline =
            new FloatOptionSpec("Heel Cut Adrenaline", 0f, 100f, 5f, 20f, OptionUnit.Points);

        public static readonly FloatOptionSpec CrippledKillAdrenaline =
            new FloatOptionSpec("Crippled Kill Adrenaline", 0f, 100f, 5f, 35f, OptionUnit.Points);

        public static readonly IReadOnlyList<FloatOptionSpec> All = new[]
        {
            HeelZoneSize,
            HeelZoneBackOffset,
            HeelZoneHeightOffset,
            MaxHitAngle,
            CrawlSpeed,
            CrawlBodyHeight,
            CrawlStartSpeed,
            CrawlStopSpeed,
            LegStrength,
            BleedOutDuration,
            BleedPulseInterval,
            WeakestCrawlSpeed,
            ScreamSlowdown,
            GaspingFrom,
            ArmStrength,
            GroundAttackChance,
            GroundAttackRange,
            GroundAttackInterval,
            ScreamInterval,
            HeelCutAdrenaline,
            CrippledKillAdrenaline,
            HitMarkerDuration,
        };
    }
}
