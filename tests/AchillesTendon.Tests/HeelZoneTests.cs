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
        [Theory]
        [InlineData(AllowedDamageTypes.SlashAndPierce, HitDamageType.Slash, true)]
        [InlineData(AllowedDamageTypes.SlashAndPierce, HitDamageType.Pierce, true)]
        [InlineData(AllowedDamageTypes.SlashAndPierce, HitDamageType.Blunt, false)]
        [InlineData(AllowedDamageTypes.SlashAndPierce, HitDamageType.Other, false)]
        [InlineData(AllowedDamageTypes.SlashOnly, HitDamageType.Slash, true)]
        [InlineData(AllowedDamageTypes.SlashOnly, HitDamageType.Pierce, false)]
        [InlineData(AllowedDamageTypes.SlashOnly, HitDamageType.Blunt, false)]
        [InlineData(AllowedDamageTypes.PierceOnly, HitDamageType.Pierce, true)]
        [InlineData(AllowedDamageTypes.PierceOnly, HitDamageType.Slash, false)]
        [InlineData(AllowedDamageTypes.PierceOnly, HitDamageType.Other, false)]
        public void IsAllowed(AllowedDamageTypes allowed, HitDamageType hit, bool expected)
        {
            Assert.Equal(expected, DamageTypeFilter.IsAllowed(allowed, hit));
        }

        [Fact]
        public void Default_enum_value_is_SlashAndPierce()
        {
            // The game's mod menu cannot match an enum default and falls back to
            // index 0, so the intended default must be the first member.
            Assert.Equal(AllowedDamageTypes.SlashAndPierce, default(AllowedDamageTypes));
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
