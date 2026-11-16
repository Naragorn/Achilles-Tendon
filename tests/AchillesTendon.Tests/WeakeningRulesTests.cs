using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class WeakeningRulesTests
    {
        /// <summary>Weakening on, bleeding, halfway, defaults-like settings.</summary>
        private static WeakeningInput Halfway()
        {
            return new WeakeningInput
            {
                FeatureOn = true,
                IsBleeding = true,
                Progress = 0.5f,
                WeakestCrawlSpeed = 0.2f,
                ScreamSlowdown = 3f,
                GaspingFrom = 0.75f,
            };
        }

        [Fact]
        public void Feature_off_leaves_the_npc_at_full_strength()
        {
            var input = Halfway();
            input.FeatureOn = false;
            input.Progress = 0.9f;

            var w = WeakeningRules.Compute(input);

            Assert.Equal(1f, w.CrawlSpeedFactor);
            Assert.Equal(1f, w.ScreamIntervalFactor);
            Assert.False(w.Gasping);
        }

        [Fact]
        public void Not_bleeding_leaves_the_npc_at_full_strength()
        {
            // Never started bleeding (not crippled, or already forgotten).
            var input = Halfway();
            input.IsBleeding = false;
            input.Progress = 0.9f;

            var w = WeakeningRules.Compute(input);

            Assert.Equal(1f, w.CrawlSpeedFactor);
            Assert.Equal(1f, w.ScreamIntervalFactor);
            Assert.False(w.Gasping);
        }

        [Fact]
        public void Fresh_cut_is_at_full_strength()
        {
            var input = Halfway();
            input.Progress = 0f;

            var w = WeakeningRules.Compute(input);

            Assert.Equal(1f, w.CrawlSpeedFactor);
            Assert.Equal(1f, w.ScreamIntervalFactor);
            Assert.False(w.Gasping);
        }

        [Fact]
        public void Halfway_is_halfway_between_full_and_weakest()
        {
            var w = WeakeningRules.Compute(Halfway());

            Assert.Equal(0.6f, w.CrawlSpeedFactor, 4);
            Assert.Equal(2f, w.ScreamIntervalFactor, 4);
            Assert.False(w.Gasping);
        }

        [Fact]
        public void At_the_end_it_reaches_the_weakest_values()
        {
            var input = Halfway();
            input.Progress = 1f;

            var w = WeakeningRules.Compute(input);

            Assert.Equal(0.2f, w.CrawlSpeedFactor, 4);
            Assert.Equal(3f, w.ScreamIntervalFactor, 4);
            Assert.True(w.Gasping);
        }

        [Fact]
        public void Progress_beyond_the_range_is_clamped()
        {
            var late = Halfway();
            late.Progress = 1.5f;
            var early = Halfway();
            early.Progress = -0.5f;

            var l = WeakeningRules.Compute(late);
            var e = WeakeningRules.Compute(early);

            Assert.Equal(0.2f, l.CrawlSpeedFactor, 4);
            Assert.Equal(3f, l.ScreamIntervalFactor, 4);
            Assert.Equal(1f, e.CrawlSpeedFactor, 4);
            Assert.Equal(1f, e.ScreamIntervalFactor, 4);
        }

        [Theory]
        [InlineData(0.74f, false)]
        [InlineData(0.75f, true)]
        [InlineData(0.9f, true)]
        public void Gasping_starts_at_the_set_point(float progress, bool gasping)
        {
            var input = Halfway();
            input.Progress = progress;

            Assert.Equal(gasping, WeakeningRules.Compute(input).Gasping);
        }

        [Fact]
        public void Gasping_from_100_percent_never_gasps_before_death()
        {
            // 100 % = the switch is effectively off: the NPC dies at 100 %.
            var input = Halfway();
            input.GaspingFrom = 1f;
            input.Progress = 0.99f;

            Assert.False(WeakeningRules.Compute(input).Gasping);
        }

        [Fact]
        public void Weakest_one_and_slowdown_one_keep_full_strength_all_the_way()
        {
            var input = Halfway();
            input.WeakestCrawlSpeed = 1f;
            input.ScreamSlowdown = 1f;
            input.Progress = 0.9f;

            var w = WeakeningRules.Compute(input);

            Assert.Equal(1f, w.CrawlSpeedFactor, 4);
            Assert.Equal(1f, w.ScreamIntervalFactor, 4);
        }
    }
}
