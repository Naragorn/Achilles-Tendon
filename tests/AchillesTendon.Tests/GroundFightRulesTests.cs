using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class GroundFightRulesTests
    {
        /// <summary>Crawling, everything on, player in reach, both timers due.</summary>
        private static GroundFightInput Ready()
        {
            return new GroundFightInput
            {
                FeatureOn = true,
                IsDown = true,
                IsCrawling = true,
                AttacksOn = true,
                IsAttacker = true,
                HasAttackClip = true,
                IsPlayingAnimation = false,
                DistanceToPlayer = 1f,
                AttackRange = 1.8f,
                SinceLastAttack = 10f,
                AttackInterval = 3f,
                ScreamsOn = true,
                SinceLastScream = 10f,
                ScreamInterval = 6f,
            };
        }

        [Fact]
        public void Ready_crawling_npc_attacks_and_screams()
        {
            var d = GroundFightRules.Decide(Ready());

            Assert.True(d.Attack);
            Assert.True(d.Scream);
        }

        [Fact]
        public void Feature_off_does_nothing()
        {
            var input = Ready();
            input.FeatureOn = false;

            var d = GroundFightRules.Decide(input);

            Assert.False(d.Attack);
            Assert.False(d.Scream);
        }

        [Fact]
        public void Not_lying_down_does_nothing()
        {
            // Getting up (e.g. released) or not yet on the ground.
            var input = Ready();
            input.IsDown = false;

            var d = GroundFightRules.Decide(input);

            Assert.False(d.Attack);
            Assert.False(d.Scream);
        }

        [Fact]
        public void Attacks_off_still_screams()
        {
            var input = Ready();
            input.AttacksOn = false;

            var d = GroundFightRules.Decide(input);

            Assert.False(d.Attack);
            Assert.True(d.Scream);
        }

        [Fact]
        public void No_attack_without_a_clip()
        {
            // Unarmed NPC: the game has no melee idle pose, so no attack clips.
            var input = Ready();
            input.HasAttackClip = false;

            Assert.False(GroundFightRules.Decide(input).Attack);
        }

        [Fact]
        public void No_attack_while_the_previous_swing_plays()
        {
            var input = Ready();
            input.IsPlayingAnimation = true;

            Assert.False(GroundFightRules.Decide(input).Attack);
        }

        [Fact]
        public void No_attack_when_the_player_is_out_of_reach()
        {
            var input = Ready();
            input.DistanceToPlayer = 1.81f;

            Assert.False(GroundFightRules.Decide(input).Attack);
        }

        [Fact]
        public void Attack_exactly_at_the_range_counts()
        {
            var input = Ready();
            input.DistanceToPlayer = 1.8f;

            Assert.True(GroundFightRules.Decide(input).Attack);
        }

        [Fact]
        public void No_attack_before_the_interval_passed()
        {
            var input = Ready();
            input.SinceLastAttack = 2.9f;

            Assert.False(GroundFightRules.Decide(input).Attack);
        }

        [Fact]
        public void Screams_off_never_screams()
        {
            var input = Ready();
            input.ScreamsOn = false;

            var d = GroundFightRules.Decide(input);

            Assert.False(d.Scream);
            Assert.True(d.Attack);
        }

        [Fact]
        public void No_scream_before_the_scream_interval_passed()
        {
            var input = Ready();
            input.SinceLastScream = 5.9f;

            Assert.False(GroundFightRules.Decide(input).Scream);
        }

        [Fact]
        public void Screams_do_not_need_the_player_in_reach()
        {
            var input = Ready();
            input.DistanceToPlayer = 50f;

            var d = GroundFightRules.Decide(input);

            Assert.True(d.Scream);
            Assert.False(d.Attack);
        }

        // Lying flat (Lie Flat Chance) the game's animation does not move the body
        // (harness step4-a, 2026-11-16): such an NPC only screams (Nadi, 2026-11-17).
        [Fact]
        public void Lying_flat_npc_never_swings_but_screams()
        {
            var input = Ready();
            input.IsCrawling = false;

            var d = GroundFightRules.Decide(input);

            Assert.False(d.Attack);
            Assert.True(d.Scream);
        }

        [Fact]
        public void Crawling_npc_with_ground_attacks_off_does_not_swing()
        {
            var input = Ready();
            input.IsCrawling = true;
            input.AttacksOn = false;

            Assert.False(GroundFightRules.Decide(input).Attack);
        }

        // Ground Attack Chance (Nadi, 2026-11-17): rolled once per NPC when its tendon is cut.
        // Attackers swing from the ground, the others only crawl and scream.
        [Fact]
        public void Npc_that_rolled_no_attacker_never_attacks_but_still_screams()
        {
            var input = Ready();
            input.IsAttacker = false;

            var d = GroundFightRules.Decide(input);

            Assert.False(d.Attack);
            Assert.True(d.Scream);
        }

        [Theory]
        [InlineData(0.75f, 0.74f, true)]
        [InlineData(0.75f, 0.75f, false)]
        [InlineData(0.75f, 0f, true)]
        [InlineData(0f, 0f, false)]
        [InlineData(1f, 1f, true)]
        [InlineData(1f, 0.99f, true)]
        public void Chance_decides_who_is_an_attacker(float chance, float roll, bool attacker)
        {
            Assert.Equal(attacker, GroundFightRules.IsAttacker(chance, roll));
        }

        [Fact]
        public void Ground_attacks_are_on_by_default_for_three_of_four_npcs()
        {
            Assert.Equal(0.75f, OptionSpecs.GroundAttackChance.Default);
            Assert.Equal("75 %", OptionSpecs.GroundAttackChance.Label(0.75f));
        }
    }
}
