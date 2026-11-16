using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class CrippleRulesShouldCrippleTests
    {
        private static HeelHitResult HeelHit() => new HeelHitResult { Rejection = HeelHitRejection.None };

        private static HeelHitResult Miss() => new HeelHitResult { Rejection = HeelHitRejection.OutsideHeelZone };

        [Fact]
        public void Heel_hit_on_a_living_npc_cripples_it()
        {
            Assert.True(CrippleRules.ShouldCripple(HeelHit(), targetIsPlayer: false, targetIsKilled: false, alreadyCrippled: false));
        }

        [Fact]
        public void A_miss_does_not_cripple()
        {
            Assert.False(CrippleRules.ShouldCripple(Miss(), targetIsPlayer: false, targetIsKilled: false, alreadyCrippled: false));
        }

        [Fact]
        public void An_already_crippled_npc_is_not_crippled_twice()
        {
            Assert.False(CrippleRules.ShouldCripple(HeelHit(), targetIsPlayer: false, targetIsKilled: false, alreadyCrippled: true));
        }

        [Fact]
        public void A_dead_npc_is_not_crippled()
        {
            Assert.False(CrippleRules.ShouldCripple(HeelHit(), targetIsPlayer: false, targetIsKilled: true, alreadyCrippled: false));
        }

        [Fact]
        public void The_player_is_not_crippled_before_the_hardcore_step()
        {
            // Player collapse is build step 6; until then a player heel hit is only reported.
            Assert.False(CrippleRules.ShouldCripple(HeelHit(), targetIsPlayer: true, targetIsKilled: false, alreadyCrippled: false));
        }
    }

    public class CrippleRulesDecideTests
    {
        private static CrippledCreatureState Down() =>
            new CrippledCreatureState { Exists = true, IsKilled = false, IsDown = true };

        private static CrippledCreatureState Up() =>
            new CrippledCreatureState { Exists = true, IsKilled = false, IsDown = false };

        [Fact]
        public void A_crippled_npc_lying_down_is_held_down()
        {
            Assert.Equal(CrippleAction.HoldDown, CrippleRules.Decide(Down(), tendonEnabled: true, standUpWhenDisabled: true, pose: DownedPose.LieFlat));
        }

        [Fact]
        public void A_crippled_npc_that_got_up_anyway_is_collapsed_again()
        {
            // E.g. burning: Brain.Stop clears every no-stand-up modifier.
            Assert.Equal(CrippleAction.Collapse, CrippleRules.Decide(Up(), tendonEnabled: true, standUpWhenDisabled: true, pose: DownedPose.LieFlat));
        }

        [Fact]
        public void A_despawned_npc_is_forgotten()
        {
            var gone = new CrippledCreatureState { Exists = false };
            Assert.Equal(CrippleAction.Forget, CrippleRules.Decide(gone, tendonEnabled: true, standUpWhenDisabled: true, pose: DownedPose.LieFlat));
        }

        [Fact]
        public void A_dead_npc_is_forgotten()
        {
            var dead = new CrippledCreatureState { Exists = true, IsKilled = true, IsDown = true };
            Assert.Equal(CrippleAction.Forget, CrippleRules.Decide(dead, tendonEnabled: true, standUpWhenDisabled: true, pose: DownedPose.LieFlat));
        }

        [Fact]
        public void A_dead_npc_is_forgotten_even_when_the_feature_is_off()
        {
            var dead = new CrippledCreatureState { Exists = true, IsKilled = true, IsDown = true };
            Assert.Equal(CrippleAction.Forget, CrippleRules.Decide(dead, tendonEnabled: false, standUpWhenDisabled: true, pose: DownedPose.LieFlat));
        }

        [Fact]
        public void Turning_the_feature_off_releases_the_npc_when_stand_up_is_wanted()
        {
            Assert.Equal(CrippleAction.Release, CrippleRules.Decide(Down(), tendonEnabled: false, standUpWhenDisabled: true, pose: DownedPose.LieFlat));
        }

        [Fact]
        public void Turning_the_feature_off_keeps_the_npc_down_when_stand_up_is_not_wanted()
        {
            Assert.Equal(CrippleAction.HoldDown, CrippleRules.Decide(Down(), tendonEnabled: false, standUpWhenDisabled: false, pose: DownedPose.LieFlat));
        }

        [Fact]
        public void With_the_feature_off_and_no_stand_up_an_npc_that_got_up_is_left_standing()
        {
            // Off means no new collapses; only the existing hold stays.
            Assert.Equal(CrippleAction.HoldDown, CrippleRules.Decide(Up(), tendonEnabled: false, standUpWhenDisabled: false, pose: DownedPose.LieFlat));
        }
    }

    public class CrippleRulesCrawlTests
    {
        private static CrippledCreatureState Lying() =>
            new CrippledCreatureState { Exists = true, IsKilled = false, IsDown = true };

        private static CrippledCreatureState Up() =>
            new CrippledCreatureState { Exists = true, IsKilled = false, IsDown = false };

        [Fact]
        public void Crawl_is_the_first_pose_so_the_menu_default_matches()
        {
            // The game's mod menu falls back to the first enum entry for enum defaults.
            Assert.Equal(0, (int)DownedPose.Crawl);
        }

        [Fact]
        public void Crawl_pose_holds_the_crawl_while_up()
        {
            // Standing ragdoll, animator in the low injured/crawl blend.
            Assert.Equal(CrippleAction.HoldCrawl, CrippleRules.Decide(Up(), tendonEnabled: true, standUpWhenDisabled: true, pose: DownedPose.Crawl));
        }

        [Fact]
        public void Crawl_pose_lets_a_knocked_down_npc_get_up_into_the_crawl()
        {
            // Knocked over by a hit: the game stands it up again, then the crawl blend takes over.
            Assert.Equal(CrippleAction.HoldCrawl, CrippleRules.Decide(Lying(), tendonEnabled: true, standUpWhenDisabled: true, pose: DownedPose.Crawl));
        }

        [Fact]
        public void Crawl_pose_with_the_feature_off_releases_when_stand_up_is_wanted()
        {
            Assert.Equal(CrippleAction.Release, CrippleRules.Decide(Up(), tendonEnabled: false, standUpWhenDisabled: true, pose: DownedPose.Crawl));
        }

        [Fact]
        public void Crawl_pose_with_the_feature_off_keeps_crawling_when_stand_up_is_not_wanted()
        {
            Assert.Equal(CrippleAction.HoldCrawl, CrippleRules.Decide(Up(), tendonEnabled: false, standUpWhenDisabled: false, pose: DownedPose.Crawl));
        }

        [Fact]
        public void Crawl_pose_forgets_the_dead()
        {
            var dead = new CrippledCreatureState { Exists = true, IsKilled = true };
            Assert.Equal(CrippleAction.Forget, CrippleRules.Decide(dead, tendonEnabled: true, standUpWhenDisabled: true, pose: DownedPose.Crawl));
        }

        [Fact]
        public void Lie_flat_with_the_feature_off_and_no_stand_up_keeps_the_hold_and_stays_flat()
        {
            Assert.Equal(CrippleAction.HoldDown, CrippleRules.Decide(Lying(), tendonEnabled: false, standUpWhenDisabled: false, pose: DownedPose.LieFlat));
        }
    }
}
