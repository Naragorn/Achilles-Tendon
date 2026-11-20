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

        // Lie Flat Chance (Nadi, 2026-11-17) replaces the Downed Pose dropdown: rolled once
        // per NPC when its tendon is cut; default 5 % lie flat, the rest crawl.
        [Theory]
        [InlineData(0.05f, 0.04f, DownedPose.LieFlat)]
        [InlineData(0.05f, 0.05f, DownedPose.Crawl)]
        [InlineData(0f, 0f, DownedPose.Crawl)]
        [InlineData(1f, 1f, DownedPose.LieFlat)]
        public void Lie_flat_chance_picks_the_pose(float chance, float roll, DownedPose pose)
        {
            Assert.Equal(pose, CrippleRules.PoseFor(chance, roll));
        }

        [Fact]
        public void Five_percent_lie_flat_by_default()
        {
            Assert.Equal(0.05f, OptionSpecs.LieFlatChance.Default);
            Assert.Equal("5 %", OptionSpecs.LieFlatChance.Label(0.05f));
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

    // Instant Death Chance (Nadi, 2026-11-20): rolled once at the cut, before the pose. A hit kills the
    // NPC with that very cut (the game's own death and kill events); the rest are crippled as before.
    public class CrippleRulesCutOutcomeTests
    {
        [Theory]
        [InlineData(0.5f, 0f, CutOutcome.InstantDeath)]
        [InlineData(0.5f, 0.49f, CutOutcome.InstantDeath)]
        [InlineData(0.5f, 0.5f, CutOutcome.Cripple)]
        [InlineData(0.5f, 1f, CutOutcome.Cripple)]
        [InlineData(0f, 0f, CutOutcome.Cripple)]
        [InlineData(1f, 1f, CutOutcome.InstantDeath)]
        public void Instant_death_chance_picks_the_outcome(float chance, float roll, CutOutcome outcome)
        {
            Assert.Equal(outcome, CrippleRules.OutcomeFor(chance, roll));
        }

        [Fact]
        public void Half_of_the_cuts_kill_by_default()
        {
            Assert.Equal(0.5f, OptionSpecs.InstantDeathChance.Default);
            Assert.Equal("50 %", OptionSpecs.InstantDeathChance.Label(0.5f));
            Assert.Equal("0 %", OptionSpecs.InstantDeathChance.Label(OptionSpecs.InstantDeathChance.Min));
            Assert.Equal("100 %", OptionSpecs.InstantDeathChance.Label(OptionSpecs.InstantDeathChance.Max));
        }

        [Fact]
        public void Instant_death_chance_is_in_the_menu_list()
        {
            Assert.Contains(OptionSpecs.InstantDeathChance, OptionSpecs.All);
        }
    }
}
