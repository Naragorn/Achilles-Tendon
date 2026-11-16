using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class GroundFightRulesTests
    {
        /// <summary>Lying, everything on, player in reach, both timers due.</summary>
        private static GroundFightInput Ready()
        {
            return new GroundFightInput
            {
                FeatureOn = true,
                IsDown = true,
                AttacksOn = true,
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
        public void Ready_npc_strengthens_its_arms_attacks_and_screams()
        {
            var d = GroundFightRules.Decide(Ready());

            Assert.True(d.StrongArms);
            Assert.True(d.Attack);
            Assert.True(d.Scream);
        }

        [Fact]
        public void Feature_off_does_nothing_and_relaxes_the_arms()
        {
            var input = Ready();
            input.FeatureOn = false;

            var d = GroundFightRules.Decide(input);

            Assert.False(d.StrongArms);
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

            Assert.False(d.StrongArms);
            Assert.False(d.Attack);
            Assert.False(d.Scream);
        }

        [Fact]
        public void Attacks_off_still_strengthens_arms_and_screams()
        {
            var input = Ready();
            input.AttacksOn = false;

            var d = GroundFightRules.Decide(input);

            Assert.True(d.StrongArms);
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
    }
}
