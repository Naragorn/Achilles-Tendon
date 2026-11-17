using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    // Leg Strength: after a crawl stops, the body rests on its shins and the leg pins
    // fold the legs into the sitting pose only slowly (CRAWL BODY log, 2026-11-17).
    public class LegPinRulesTests
    {
        [Fact]
        public void X1_keeps_the_game_values()
        {
            var pin = CrippleRules.LegPin(1f, spring: 800f, damper: 50f, maxForce: 100f);

            Assert.Equal(800f, pin.Spring);
            Assert.Equal(50f, pin.Damper);
            Assert.Equal(100f, pin.MaxForce);
        }

        [Fact]
        public void Spring_and_max_force_scale_with_the_multiplier()
        {
            var pin = CrippleRules.LegPin(4f, spring: 800f, damper: 50f, maxForce: 100f);

            Assert.Equal(3200f, pin.Spring);
            Assert.Equal(400f, pin.MaxForce);
        }

        [Fact]
        public void Damper_scales_with_the_square_root_to_keep_the_damping_ratio()
        {
            // Critical damping grows with sqrt(spring); scaling it the same way keeps the
            // stronger pin from overshooting and shaking.
            var pin = CrippleRules.LegPin(4f, spring: 800f, damper: 50f, maxForce: 100f);

            Assert.Equal(100f, pin.Damper, 3);
        }

        [Fact]
        public void A_multiplier_below_one_is_treated_as_one()
        {
            var pin = CrippleRules.LegPin(0.5f, spring: 800f, damper: 50f, maxForce: 100f);

            Assert.Equal(800f, pin.Spring);
            Assert.Equal(50f, pin.Damper);
            Assert.Equal(100f, pin.MaxForce);
        }
    }
}
