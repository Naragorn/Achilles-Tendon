using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class Vec3Tests
    {
        [Fact]
        public void Arithmetic_works_component_wise()
        {
            var a = new Vec3(1f, 2f, 3f);
            var b = new Vec3(0.5f, -1f, 2f);

            Assert.Equal(new Vec3(1.5f, 1f, 5f), a + b);
            Assert.Equal(new Vec3(0.5f, 3f, 1f), a - b);
            Assert.Equal(new Vec3(2f, 4f, 6f), a * 2f);
            Assert.Equal(new Vec3(-1f, -2f, -3f), -a);
        }

        [Fact]
        public void Dot_and_magnitude()
        {
            var a = new Vec3(3f, 4f, 0f);

            Assert.Equal(25f, Vec3.Dot(a, a));
            Assert.Equal(5f, a.Magnitude, 5);
        }

        [Fact]
        public void Flattened_drops_the_vertical_component()
        {
            Assert.Equal(new Vec3(1f, 0f, 2f), new Vec3(1f, 7f, 2f).Flattened);
        }

        [Fact]
        public void Normalized_of_zero_vector_is_zero()
        {
            Assert.Equal(Vec3.Zero, Vec3.Zero.Normalized);
        }

        [Fact]
        public void Normalized_has_unit_length()
        {
            Assert.Equal(1f, new Vec3(0f, 0f, 9f).Normalized.Magnitude, 5);
        }

        [Theory]
        [InlineData(0f, 0f, 1f, 0f)]
        [InlineData(1f, 0f, 0f, 90f)]
        [InlineData(0f, 0f, -1f, 180f)]
        [InlineData(1f, 0f, 1f, 45f)]
        public void AngleDegrees_between_forward_and_direction(float x, float y, float z, float expected)
        {
            var angle = Vec3.AngleDegrees(new Vec3(0f, 0f, 1f), new Vec3(x, y, z));

            Assert.Equal(expected, angle, 3);
        }

        [Fact]
        public void AngleDegrees_with_zero_vector_is_NaN()
        {
            Assert.True(float.IsNaN(Vec3.AngleDegrees(Vec3.Zero, new Vec3(0f, 0f, 1f))));
            Assert.True(float.IsNaN(Vec3.AngleDegrees(new Vec3(0f, 0f, 1f), Vec3.Zero)));
        }
    }
}
