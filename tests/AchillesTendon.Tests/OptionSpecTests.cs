using System.Collections.Generic;
using System.Linq;
using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class OptionSpecTests
    {
        public static IEnumerable<object[]> AllSpecs()
        {
            return OptionSpecs.All.Select(spec => new object[] { spec.Name, spec });
        }

        [Theory]
        [MemberData(nameof(AllSpecs))]
        public void Default_is_one_of_the_values_exactly(string name, FloatOptionSpec spec)
        {
            // The game's mod menu finds the default with object.Equals on the
            // boxed float, so the default must be bit-identical to one entry.
            Assert.Contains(spec.Values(), v => v.Equals(spec.Default));
            Assert.False(string.IsNullOrEmpty(name));
        }

        [Theory]
        [MemberData(nameof(AllSpecs))]
        public void Values_run_from_min_to_max_in_steps(string name, FloatOptionSpec spec)
        {
            var values = spec.Values();

            Assert.Equal(spec.Min, values.First());
            Assert.Equal(spec.Max, values.Last(), 4);
            for (var i = 1; i < values.Length; i++)
            {
                Assert.Equal(spec.Step, values[i] - values[i - 1], 4);
            }
            Assert.False(string.IsNullOrEmpty(name));
        }

        [Fact]
        public void Values_have_no_float_drift()
        {
            var spec = new FloatOptionSpec("Test", 0.02f, 0.2f, 0.01f, 0.08f, OptionUnit.Centimeters);

            Assert.Contains(spec.Values(), v => v.Equals(0.08f));
            Assert.Contains(spec.Values(), v => v.Equals(0.2f));
            Assert.Equal(19, spec.Values().Length);
        }

        [Theory]
        [InlineData(OptionUnit.Centimeters, 0.08f, "8 cm")]
        [InlineData(OptionUnit.Centimeters, -0.05f, "-5 cm")]
        [InlineData(OptionUnit.Degrees, 110f, "110°")]
        [InlineData(OptionUnit.Seconds, 5f, "5 s")]
        [InlineData(OptionUnit.Seconds, 0.5f, "0.5 s")]
        public void Label(OptionUnit unit, float value, string expected)
        {
            var spec = new FloatOptionSpec("Test", 0f, 1f, 0.1f, 0f, unit);

            Assert.Equal(expected, spec.Label(value));
        }

        [Fact]
        public void Specs_have_unique_names()
        {
            var names = OptionSpecs.All.Select(s => s.Name).ToList();

            Assert.Equal(names.Count, names.Distinct().Count());
        }
    }
}
