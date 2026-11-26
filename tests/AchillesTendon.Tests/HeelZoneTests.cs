using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class HeelZoneTests
    {
        [Fact]
        public void Center_sits_behind_and_above_the_ankle()
        {
            var center = HeelZone.Center(Vec3.Zero, new Vec3(0f, 0f, 1f), 0.04f, 0.03f);

            Assert.Equal(0f, center.X, 5);
            Assert.Equal(0.03f, center.Y, 5);
            Assert.Equal(-0.04f, center.Z, 5);
        }

        [Fact]
        public void Center_ignores_the_vertical_part_of_the_facing()
        {
            // Creature looks down at 45 degrees: "behind" must stay horizontal.
            var center = HeelZone.Center(new Vec3(1f, 0f, 1f), new Vec3(0f, -1f, 1f), 0.1f, 0f);

            Assert.Equal(1f, center.X, 5);
            Assert.Equal(0f, center.Y, 5);
            Assert.Equal(0.9f, center.Z, 5);
        }

        [Fact]
        public void Center_without_horizontal_facing_gets_no_back_offset()
        {
            var center = HeelZone.Center(Vec3.Zero, new Vec3(0f, 1f, 0f), 0.1f, 0.02f);

            Assert.Equal(new Vec3(0f, 0.02f, 0f), center);
        }
    }

    public class DamageTypeFilterTests
    {
        // Only a blade cuts a tendon: slash and pierce, never blunt (Allowed Damage Types removed, v0.8.11).
        [Theory]
        [InlineData(HitDamageType.Slash, true)]
        [InlineData(HitDamageType.Pierce, true)]
        [InlineData(HitDamageType.Blunt, false)]
        [InlineData(HitDamageType.Other, false)]
        public void IsAllowed(HitDamageType hit, bool expected)
        {
            Assert.Equal(expected, DamageTypeFilter.IsAllowed(hit));
        }
    }

    public class AttackAngleTests
    {
        private static readonly Vec3 Forward = new Vec3(0f, 0f, 1f);

        [Theory]
        [InlineData(0f, 0f, -2f, 0f)]    // directly behind
        [InlineData(2f, 0f, 0f, 90f)]    // right side
        [InlineData(-2f, 0f, 0f, 90f)]   // left side
        [InlineData(0f, 0f, 2f, 180f)]   // in front
        [InlineData(1f, 5f, -1f, 45f)]   // height of attacker is ignored
        public void FromBehind(float x, float y, float z, float expected)
        {
            var angle = AttackAngle.FromBehind(Vec3.Zero, Forward, new Vec3(x, y, z));

            Assert.Equal(expected, angle, 3);
        }

        [Fact]
        public void FromBehind_is_NaN_when_attacker_stands_on_the_target()
        {
            Assert.True(float.IsNaN(AttackAngle.FromBehind(Vec3.Zero, Forward, new Vec3(0f, 1f, 0f))));
        }

        [Fact]
        public void FromBehind_is_NaN_when_target_has_no_horizontal_facing()
        {
            Assert.True(float.IsNaN(AttackAngle.FromBehind(Vec3.Zero, new Vec3(0f, 1f, 0f), new Vec3(0f, 0f, -1f))));
        }
    }
}
