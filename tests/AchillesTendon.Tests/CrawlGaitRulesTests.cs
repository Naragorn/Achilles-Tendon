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
    }
}
