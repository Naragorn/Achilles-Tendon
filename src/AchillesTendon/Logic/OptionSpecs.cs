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

        public static readonly FloatOptionSpec HitMarkerDuration =
            new FloatOptionSpec("Hit Marker Duration", 1f, 30f, 1f, 5f, OptionUnit.Seconds);

        public static readonly IReadOnlyList<FloatOptionSpec> All = new[]
        {
            HeelZoneSize,
            HeelZoneBackOffset,
            HeelZoneHeightOffset,
            MaxHitAngle,
            HitMarkerDuration,
        };
    }
}
