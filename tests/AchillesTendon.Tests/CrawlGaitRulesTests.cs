using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    // The crawl pose's idle (sitting) and moving (crawling) animator states switch at
    // |Speed| or |Strafe| = 0.1 with no hysteresis; the AI's small corrective moves sit
    // right at it. CrawlGaitRules adds the hysteresis: start above Start, stop below Stop.
    public class CrawlGaitRulesTests
    {
        private const float Start = 0.25f;
        private const float Stop = 0.1f;

        [Theory]
        [InlineData(0f, 0f)]
        [InlineData(0.17f, 0f)]
        [InlineData(-0.13f, 0.12f)]
        [InlineData(0.24f, -0.24f)]
        public void Still_stays_still_below_start(float speed, float strafe)
        {
            Assert.False(CrawlGaitRules.IsMoving(wasMoving: false, speed, strafe, Start, Stop));
        }

        [Theory]
        [InlineData(0.25f, 0f)]
        [InlineData(0.5f, 0f)]
        [InlineData(-0.3f, 0f)]
        [InlineData(0f, 0.3f)]
        [InlineData(0f, -0.3f)]
        public void Still_starts_moving_at_start_forward_backward_or_sideways(float speed, float strafe)
        {
            Assert.True(CrawlGaitRules.IsMoving(wasMoving: false, speed, strafe, Start, Stop));
        }

        [Theory]
        [InlineData(0.5f, 0f)]
        [InlineData(0.17f, 0f)]
        [InlineData(0.1f, 0f)]
        [InlineData(0f, -0.15f)]
        public void Moving_keeps_moving_down_to_stop(float speed, float strafe)
        {
            Assert.True(CrawlGaitRules.IsMoving(wasMoving: true, speed, strafe, Start, Stop));
        }

        [Theory]
        [InlineData(0.09f, 0f)]
        [InlineData(-0.05f, 0.05f)]
        [InlineData(0f, 0f)]
        public void Moving_stops_below_stop(float speed, float strafe)
        {
            Assert.False(CrawlGaitRules.IsMoving(wasMoving: true, speed, strafe, Start, Stop));
        }

        [Fact]
        public void Stop_above_start_is_capped_at_start()
        {
            // Misconfigured menu (Stop 0.4 > Start 0.25): no gap between the two, so
            // 0.3 keeps moving instead of flipping every frame.
            Assert.True(CrawlGaitRules.IsMoving(wasMoving: true, 0.3f, 0f, Start, 0.4f));
            Assert.False(CrawlGaitRules.IsMoving(wasMoving: true, 0.2f, 0f, Start, 0.4f));
        }

        [Fact]
        public void Spec_defaults_leave_a_gap_around_the_animator_threshold()
        {
            Assert.True(OptionSpecs.CrawlStartSpeed.Default > 0.1f);
            Assert.True(OptionSpecs.CrawlStopSpeed.Default <= 0.1f);
            Assert.True(OptionSpecs.CrawlStopSpeed.Default < OptionSpecs.CrawlStartSpeed.Default);
        }

        // Belly crawl (Nadi, 2026-11-17): the game's forward Crawling clip lies face down;
        // the idle states (Laying/LayingInjured) and CrawlBackwards lie on the back. A belly
        // crawler is therefore always kept in the moving state on the forward crawl: still
        // at a speed just above the animator's idle switch (0.1), moving at its own speed
        // forward, whichever way it moves.
        [Fact]
        public void Still_belly_crawler_is_held_just_above_the_idle_switch()
        {
            var gait = CrawlGaitRules.BellyGait(moving: false, speed: 0.02f, strafe: -0.05f);

            Assert.Equal(CrawlGaitRules.BellyHoldSpeed, gait.Speed);
            Assert.Equal(0f, gait.Strafe);
            Assert.True(CrawlGaitRules.BellyHoldSpeed > 0.1f);
        }

        [Theory]
        [InlineData(0.4f, 0f, 0.4f)]
        [InlineData(-0.4f, 0.1f, 0.4f)]
        [InlineData(0.05f, -0.5f, 0.5f)]
        public void Moving_belly_crawler_always_plays_the_forward_crawl(float speed, float strafe, float expected)
        {
            var gait = CrawlGaitRules.BellyGait(moving: true, speed, strafe);

            Assert.Equal(expected, gait.Speed, 3);
            Assert.Equal(0f, gait.Strafe);
        }

        [Fact]
        public void Slow_moving_belly_crawler_never_drops_below_the_hold_speed()
        {
            Assert.Equal(CrawlGaitRules.BellyHoldSpeed, CrawlGaitRules.BellyGait(moving: true, speed: 0.05f, strafe: 0f).Speed);
        }

        [Fact]
        public void Belly_crawl_is_off_by_default_until_it_looks_right()
        {
            // Probes 20261117-192804 and -195433: the NPC falls forward but then kneels
            // upright instead of lying face down (Nadi decides how to go on).
            Assert.Equal(0f, OptionSpecs.BellyCrawlChance.Default);
        }
    }
}
