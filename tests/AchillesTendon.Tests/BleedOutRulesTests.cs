using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class BleedOutRulesTests
    {
        private static BleedInput Bleeding(float health = 100f, float elapsed = 0f, int lastPulse = 0, float dt = 1f)
        {
            return new BleedInput
            {
                Exists = true,
                IsKilled = false,
                FeatureOn = true,
                Health = health,
                Elapsed = elapsed,
                LastPulseIndex = lastPulse,
                DeltaTime = dt,
                Duration = 40f,
                PulseInterval = 2f,
            };
        }

        [Fact]
        public void A_despawned_npc_is_forgotten()
        {
            var input = Bleeding();
            input.Exists = false;

            Assert.Equal(BleedAction.Forget, BleedOutRules.Step(input).Action);
        }

        [Fact]
        public void A_dead_npc_is_forgotten_even_with_the_feature_off()
        {
            // Finished off by the player earlier: nothing left to bleed.
            var input = Bleeding();
            input.IsKilled = true;
            input.FeatureOn = false;

            Assert.Equal(BleedAction.Forget, BleedOutRules.Step(input).Action);
        }

        [Fact]
        public void With_the_feature_off_the_bleeding_pauses_and_keeps_its_progress()
        {
            var input = Bleeding(health: 50f, elapsed: 20f, lastPulse: 10);
            input.FeatureOn = false;

            var step = BleedOutRules.Step(input);

            Assert.Equal(BleedAction.Pause, step.Action);
            Assert.Equal(20f, step.Elapsed);
            Assert.Equal(50f, step.Health);
            Assert.False(step.Pulse);
        }

        [Fact]
        public void Health_drains_linearly_to_zero_at_the_end()
        {
            // 40 s duration, full health 100: after 1 s, 97.5 left.
            var step = BleedOutRules.Step(Bleeding(health: 100f, elapsed: 0f, dt: 1f));

            Assert.Equal(BleedAction.Continue, step.Action);
            Assert.Equal(97.5f, step.Health, 3);
            Assert.Equal(1f, step.Elapsed, 5);
        }

        [Fact]
        public void Drain_continues_from_the_current_health_after_extra_damage()
        {
            // The player hit it to 30 health at 10 s: the remaining 30 s drain those 30.
            var step = BleedOutRules.Step(Bleeding(health: 30f, elapsed: 10f, lastPulse: 5, dt: 1f));

            Assert.Equal(29f, step.Health, 3);
        }

        [Fact]
        public void Reaching_the_duration_kills()
        {
            var step = BleedOutRules.Step(Bleeding(health: 1f, elapsed: 39.5f, lastPulse: 19, dt: 0.5f));

            Assert.Equal(BleedAction.Kill, step.Action);
            Assert.Equal(0f, step.Health);
        }

        [Fact]
        public void Shortening_the_duration_below_the_elapsed_time_kills_at_once()
        {
            // The duration slider is live: 40 s already bled, the slider moved to 30 s.
            var input = Bleeding(health: 10f, elapsed: 35f, lastPulse: 17, dt: 0.02f);
            input.Duration = 30f;

            Assert.Equal(BleedAction.Kill, BleedOutRules.Step(input).Action);
        }

        [Fact]
        public void No_health_left_kills_even_before_the_duration()
        {
            var step = BleedOutRules.Step(Bleeding(health: 0f, elapsed: 5f, lastPulse: 2));

            Assert.Equal(BleedAction.Kill, step.Action);
        }

        [Fact]
        public void The_first_frame_pulses_at_once()
        {
            // A new bleeding starts with LastPulseIndex = BleedOutRules.NoPulseYet.
            var step = BleedOutRules.Step(Bleeding(lastPulse: BleedOutRules.NoPulseYet, dt: 0.02f));

            Assert.True(step.Pulse);
            Assert.Equal(0, step.PulseIndex);
        }

        [Fact]
        public void No_pulse_until_the_next_interval_starts()
        {
            var step = BleedOutRules.Step(Bleeding(elapsed: 2.5f, lastPulse: 1, dt: 1f));

            Assert.False(step.Pulse);
            Assert.Equal(1, step.PulseIndex);
        }

        [Fact]
        public void A_new_interval_pulses()
        {
            var step = BleedOutRules.Step(Bleeding(elapsed: 3.5f, lastPulse: 1, dt: 1f));

            Assert.True(step.Pulse);
            Assert.Equal(2, step.PulseIndex);
        }

        [Fact]
        public void Pulse_intensity_grows_towards_one_at_the_end()
        {
            // 20 pulses over 40 s: the first is 1/20, the last is 1.
            var first = BleedOutRules.Step(Bleeding(lastPulse: BleedOutRules.NoPulseYet, dt: 0.02f));
            var last = BleedOutRules.Step(Bleeding(health: 5f, elapsed: 38.5f, lastPulse: 18, dt: 0.5f));

            Assert.Equal(0.05f, first.PulseIntensity, 4);
            Assert.True(last.Pulse);
            Assert.Equal(1f, last.PulseIntensity, 4);
        }

        [Fact]
        public void Pulse_intensity_never_exceeds_one()
        {
            // Interval longer than the duration.
            var input = Bleeding(lastPulse: BleedOutRules.NoPulseYet, dt: 0.02f);
            input.PulseInterval = 60f;

            Assert.Equal(1f, BleedOutRules.Step(input).PulseIntensity);
        }

        [Fact]
        public void Kill_frame_does_not_pulse()
        {
            var step = BleedOutRules.Step(Bleeding(health: 1f, elapsed: 39.9f, lastPulse: 19, dt: 0.2f));

            Assert.Equal(BleedAction.Kill, step.Action);
            Assert.False(step.Pulse);
        }
    }
}
