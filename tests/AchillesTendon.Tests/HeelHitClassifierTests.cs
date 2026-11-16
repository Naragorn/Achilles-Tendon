using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class HeelHitClassifierTests
    {
        private static readonly Vec3 Ankle = new Vec3(0.1f, 0.08f, 0f);

        // Zone center for the default settings below: ankle + (0, 0.03, -0.04).
        private static readonly Vec3 ZoneCenter = new Vec3(0.1f, 0.11f, -0.04f);

        private static HeelHitSettings Settings(
            AllowedDamageTypes allowed = AllowedDamageTypes.SlashAndPierce,
            float maxHitAngle = 110f,
            bool applyToPlayer = false)
        {
            return new HeelHitSettings
            {
                ZoneRadius = 0.08f,
                ZoneBackOffset = 0.04f,
                ZoneHeightOffset = 0.03f,
                MaxHitAngle = maxHitAngle,
                AllowedDamageTypes = allowed,
                ApplyToPlayer = applyToPlayer,
            };
        }

        /// <summary>An NPC slashes the player's left heel from directly behind.</summary>
        private static HeelHitInput NpcSlashesPlayerHeel()
        {
            var input = HeelSlashFromBehind();
            input.AttackerIsPlayer = false;
            input.TargetIsPlayer = true;
            return input;
        }

        /// <summary>Player slashes the left heel of an NPC from directly behind.</summary>
        private static HeelHitInput HeelSlashFromBehind()
        {
            return new HeelHitInput
            {
                AttackerIsPlayer = true,
                TargetIsPlayer = false,
                Side = LegSide.Left,
                DamageType = HitDamageType.Slash,
                ContactPoint = ZoneCenter,
                HasAnkle = true,
                Ankle = Ankle,
                TargetPosition = Vec3.Zero,
                TargetForward = new Vec3(0f, 0f, 1f),
                HasAttackerPosition = true,
                AttackerPosition = new Vec3(0f, 0f, -1f),
            };
        }

        [Fact]
        public void Slash_on_the_heel_from_behind_is_a_heel_hit()
        {
            var result = HeelHitClassifier.Classify(HeelSlashFromBehind(), Settings());

            Assert.True(result.IsHeelHit);
            Assert.Equal(HeelHitRejection.None, result.Rejection);
            Assert.Equal(0f, result.DistanceToZoneCenter, 4);
            Assert.Equal(0f, result.AttackAngle, 3);
            Assert.True(result.HasZone);
            Assert.Equal(ZoneCenter.X, result.ZoneCenter.X, 5);
            Assert.Equal(ZoneCenter.Y, result.ZoneCenter.Y, 5);
            Assert.Equal(ZoneCenter.Z, result.ZoneCenter.Z, 5);
        }

        [Fact]
        public void Pierce_on_the_heel_is_a_heel_hit_by_default()
        {
            var input = HeelSlashFromBehind();
            input.DamageType = HitDamageType.Pierce;

            Assert.True(HeelHitClassifier.Classify(input, Settings()).IsHeelHit);
        }

        [Fact]
        public void Right_leg_works_the_same()
        {
            var input = HeelSlashFromBehind();
            input.Side = LegSide.Right;

            Assert.True(HeelHitClassifier.Classify(input, Settings()).IsHeelHit);
        }

        [Fact]
        public void Hit_by_an_npc_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.AttackerIsPlayer = false;

            Assert.Equal(HeelHitRejection.NotPlayerVsNpc, HeelHitClassifier.Classify(input, Settings()).Rejection);
        }

        [Fact]
        public void Hit_on_the_player_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.TargetIsPlayer = true;

            Assert.Equal(HeelHitRejection.NotPlayerVsNpc, HeelHitClassifier.Classify(input, Settings()).Rejection);
        }

        [Fact]
        public void Hit_on_a_non_leg_part_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.Side = LegSide.None;

            var result = HeelHitClassifier.Classify(input, Settings());

            Assert.Equal(HeelHitRejection.NotLegPart, result.Rejection);
            Assert.False(result.HasZone);
            Assert.True(float.IsNaN(result.DistanceToZoneCenter));
        }

        [Fact]
        public void Leg_without_a_foot_bone_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.HasAnkle = false;

            var result = HeelHitClassifier.Classify(input, Settings());

            Assert.Equal(HeelHitRejection.NoAnkle, result.Rejection);
            Assert.False(result.HasZone);
        }

        [Fact]
        public void Blunt_hit_on_the_heel_is_rejected_but_still_measured()
        {
            var input = HeelSlashFromBehind();
            input.DamageType = HitDamageType.Blunt;

            var result = HeelHitClassifier.Classify(input, Settings());

            Assert.Equal(HeelHitRejection.DamageTypeNotAllowed, result.Rejection);
            Assert.Equal(0f, result.DistanceToZoneCenter, 4);
            Assert.Equal(0f, result.AttackAngle, 3);
        }

        [Fact]
        public void Pierce_is_rejected_when_only_slash_is_allowed()
        {
            var input = HeelSlashFromBehind();
            input.DamageType = HitDamageType.Pierce;

            Assert.Equal(
                HeelHitRejection.DamageTypeNotAllowed,
                HeelHitClassifier.Classify(input, Settings(AllowedDamageTypes.SlashOnly)).Rejection);
        }

        [Fact]
        public void Slash_is_rejected_when_only_pierce_is_allowed()
        {
            Assert.Equal(
                HeelHitRejection.DamageTypeNotAllowed,
                HeelHitClassifier.Classify(HeelSlashFromBehind(), Settings(AllowedDamageTypes.PierceOnly)).Rejection);
        }

        [Fact]
        public void Contact_outside_the_zone_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.ContactPoint = ZoneCenter + new Vec3(0f, 0.081f, 0f);

            var result = HeelHitClassifier.Classify(input, Settings());

            Assert.Equal(HeelHitRejection.OutsideHeelZone, result.Rejection);
            Assert.Equal(0.081f, result.DistanceToZoneCenter, 4);
        }

        [Fact]
        public void Contact_exactly_on_the_zone_border_counts()
        {
            var input = HeelSlashFromBehind();
            input.ContactPoint = ZoneCenter + new Vec3(0f, 0f, -0.08f);

            Assert.True(HeelHitClassifier.Classify(input, Settings()).IsHeelHit);
        }

        [Fact]
        public void Attack_from_the_front_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.AttackerPosition = new Vec3(0f, 0f, 1f);

            var result = HeelHitClassifier.Classify(input, Settings());

            Assert.Equal(HeelHitRejection.AttackAngleTooFrontal, result.Rejection);
            Assert.Equal(180f, result.AttackAngle, 3);
        }

        [Fact]
        public void Attack_from_the_side_counts_with_the_default_angle()
        {
            var input = HeelSlashFromBehind();
            input.AttackerPosition = new Vec3(1f, 0f, 0f);

            Assert.True(HeelHitClassifier.Classify(input, Settings()).IsHeelHit);
        }

        [Fact]
        public void Attack_exactly_at_the_max_angle_counts()
        {
            var input = HeelSlashFromBehind();
            input.AttackerPosition = new Vec3(1f, 0f, 0f); // 90 degrees from behind

            Assert.True(HeelHitClassifier.Classify(input, Settings(maxHitAngle: 90f)).IsHeelHit);
        }

        [Fact]
        public void Attack_just_past_the_max_angle_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.AttackerPosition = new Vec3(1f, 0f, 0.01f); // slightly more than 90 degrees

            Assert.Equal(
                HeelHitRejection.AttackAngleTooFrontal,
                HeelHitClassifier.Classify(input, Settings(maxHitAngle: 90f)).Rejection);
        }

        [Fact]
        public void Unknown_attacker_position_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.HasAttackerPosition = false;

            var result = HeelHitClassifier.Classify(input, Settings());

            Assert.Equal(HeelHitRejection.AttackAngleUndetermined, result.Rejection);
            Assert.True(float.IsNaN(result.AttackAngle));
        }

        [Fact]
        public void Attacker_standing_on_the_target_is_rejected()
        {
            var input = HeelSlashFromBehind();
            input.AttackerPosition = new Vec3(0f, 1.8f, 0f);

            Assert.Equal(
                HeelHitRejection.AttackAngleUndetermined,
                HeelHitClassifier.Classify(input, Settings()).Rejection);
        }

        [Fact]
        public void Npc_hitting_itself_is_a_self_hit()
        {
            // A lying NPC whose own weapon touches its own body: attacker = target.
            var input = HeelSlashFromBehind();
            input.AttackerIsPlayer = false;
            input.AttackerIsTarget = true;

            Assert.Equal(HeelHitRejection.SelfHit, HeelHitClassifier.Classify(input, Settings()).Rejection);
        }

        [Fact]
        public void Player_hitting_itself_is_a_self_hit_even_with_apply_to_player()
        {
            var input = HeelSlashFromBehind();
            input.TargetIsPlayer = true;
            input.AttackerIsTarget = true;

            Assert.Equal(
                HeelHitRejection.SelfHit,
                HeelHitClassifier.Classify(input, Settings(applyToPlayer: true)).Rejection);
        }

        [Fact]
        public void Self_hit_check_wins_over_every_later_check()
        {
            var input = HeelSlashFromBehind();
            input.AttackerIsPlayer = false;
            input.AttackerIsTarget = true;
            input.Side = LegSide.None;
            input.DamageType = HitDamageType.Blunt;

            Assert.Equal(HeelHitRejection.SelfHit, HeelHitClassifier.Classify(input, Settings()).Rejection);
        }

        [Fact]
        public void Player_check_wins_over_every_later_check()
        {
            var input = HeelSlashFromBehind();
            input.AttackerIsPlayer = false;
            input.Side = LegSide.None;
            input.DamageType = HitDamageType.Blunt;

            Assert.Equal(HeelHitRejection.NotPlayerVsNpc, HeelHitClassifier.Classify(input, Settings()).Rejection);
        }

        [Fact]
        public void Zone_check_comes_before_the_angle_check()
        {
            var input = HeelSlashFromBehind();
            input.ContactPoint = ZoneCenter + new Vec3(0f, 1f, 0f);
            input.AttackerPosition = new Vec3(0f, 0f, 1f);

            Assert.Equal(HeelHitRejection.OutsideHeelZone, HeelHitClassifier.Classify(input, Settings()).Rejection);
        }
        [Fact]
        public void Npc_hitting_the_player_heel_is_rejected_by_default()
        {
            Assert.Equal(
                HeelHitRejection.NotPlayerVsNpc,
                HeelHitClassifier.Classify(NpcSlashesPlayerHeel(), Settings()).Rejection);
        }

        [Fact]
        public void Npc_hitting_the_player_heel_counts_with_apply_to_player()
        {
            var result = HeelHitClassifier.Classify(NpcSlashesPlayerHeel(), Settings(applyToPlayer: true));

            Assert.True(result.IsHeelHit);
        }

        [Fact]
        public void Apply_to_player_runs_the_same_checks_for_the_player()
        {
            var input = NpcSlashesPlayerHeel();
            input.DamageType = HitDamageType.Blunt;

            Assert.Equal(
                HeelHitRejection.DamageTypeNotAllowed,
                HeelHitClassifier.Classify(input, Settings(applyToPlayer: true)).Rejection);
        }

        [Fact]
        public void Hit_on_the_player_from_an_unknown_source_is_not_an_npc_hit()
        {
            // No resolvable attacker (trap, falling object, lost handler): it is
            // not an NPC hit, so it is out of scope like NPC vs NPC.
            var input = NpcSlashesPlayerHeel();
            input.HasAttackerPosition = false;

            Assert.Equal(
                HeelHitRejection.NotPlayerVsNpc,
                HeelHitClassifier.Classify(input, Settings(applyToPlayer: true)).Rejection);
        }

        [Fact]
        public void Player_hitting_own_heel_is_rejected_even_with_apply_to_player()
        {
            var input = HeelSlashFromBehind();
            input.TargetIsPlayer = true;

            Assert.Equal(
                HeelHitRejection.NotPlayerVsNpc,
                HeelHitClassifier.Classify(input, Settings(applyToPlayer: true)).Rejection);
        }

        [Fact]
        public void Npc_vs_npc_is_rejected_even_with_apply_to_player()
        {
            var input = HeelSlashFromBehind();
            input.AttackerIsPlayer = false;

            Assert.Equal(
                HeelHitRejection.NotPlayerVsNpc,
                HeelHitClassifier.Classify(input, Settings(applyToPlayer: true)).Rejection);
        }

        [Fact]
        public void Player_hitting_an_npc_still_counts_with_apply_to_player()
        {
            Assert.True(HeelHitClassifier.Classify(HeelSlashFromBehind(), Settings(applyToPlayer: true)).IsHeelHit);
        }
    }
}
