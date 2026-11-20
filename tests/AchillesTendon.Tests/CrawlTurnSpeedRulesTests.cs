using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    // Crawl Turn Speed (Nadi, 2026-11-20): at headset pace (90 fps) the game's AI turns a
    // crippled NPC half around in 0.17 s, as fast as a standing one (run 20261120-112856).
    public class CrawlTurnSpeedRulesTests
    {
        [Fact]
        public void Hundred_percent_keeps_the_game_value()
        {
            Assert.Equal(2f, CrippleRules.CrawlTurnSpeed(2f, 1f));
        }

        [Fact]
        public void The_factor_scales_the_game_value()
        {
            Assert.Equal(0.5f, CrippleRules.CrawlTurnSpeed(2f, 0.25f), 5);
        }

        [Fact]
        public void Above_hundred_percent_counts_as_hundred()
        {
            // Never faster than the game's own turn.
            Assert.Equal(2f, CrippleRules.CrawlTurnSpeed(2f, 1.5f));
        }

        [Fact]
        public void Below_the_menu_minimum_counts_as_the_minimum()
        {
            // An NPC that can hardly turn would never face the player, so it would never swing.
            var min = OptionSpecs.CrawlTurnSpeed.Min;

            Assert.Equal(2f * min, CrippleRules.CrawlTurnSpeed(2f, 0f), 5);
        }

        [Fact]
        public void The_menu_defaults_to_a_quarter_and_stops_at_the_game_speed()
        {
            Assert.Equal(0.25f, OptionSpecs.CrawlTurnSpeed.Default);
            Assert.Equal(1f, OptionSpecs.CrawlTurnSpeed.Max);
            Assert.True(OptionSpecs.CrawlTurnSpeed.Min > 0f);
            Assert.Contains(OptionSpecs.CrawlTurnSpeed, OptionSpecs.All);
        }
    }
}
