using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    // A crawling NPC is a standing ragdoll, so the game's BrainModuleDeath plays a standing
    // death clip with fading pins for about 3 s: the body hangs stiffly above the ground
    // (probe run 20261117-180857, DEATH POSE). Achilles stops that dying animation.
    public class DeathRulesTests
    {
        [Fact]
        public void Crawling_npc_in_the_dying_animation_is_stopped()
        {
            Assert.True(DeathRules.StopDyingAnimation(wasCrawling: true, isDying: true));
        }

        [Fact]
        public void Crawling_npc_without_a_dying_animation_is_left_alone()
        {
            Assert.False(DeathRules.StopDyingAnimation(wasCrawling: true, isDying: false));
        }

        [Fact]
        public void Npc_that_was_not_crawling_keeps_the_games_death()
        {
            Assert.False(DeathRules.StopDyingAnimation(wasCrawling: false, isDying: true));
            Assert.False(DeathRules.StopDyingAnimation(wasCrawling: false, isDying: false));
        }
    }
}
