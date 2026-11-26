namespace AchillesTendon.Logic
{
    /// <summary>Damage type of a hit, reduced to what the heel check cares about.</summary>
    public enum HitDamageType
    {
        Other,
        Slash,
        Pierce,
        Blunt,
    }

    /// <summary>Side of the leg that was hit, or None if the part is not a leg or foot.</summary>
    public enum LegSide
    {
        None,
        Left,
        Right,
    }

    /// <summary>Why a hit was not counted as a heel hit. Checked in this order.</summary>
    public enum HeelHitRejection
    {
        None,

        /// <summary>
        /// The creature hit itself (attacker = target), e.g. a lying NPC whose own
        /// weapon touches its body. Never counts and is not logged.
        /// </summary>
        SelfHit,

        /// <summary>
        /// Not the player hitting an NPC: NPC vs NPC, any hit on the player, or no known attacker.
        /// </summary>
        NotPlayerVsNpc,
        NotLegPart,
        NoAnkle,
        DamageTypeNotAllowed,
        OutsideHeelZone,
        AttackAngleUndetermined,
        AttackAngleTooFrontal,
    }

    /// <summary>Tuning values, all of them Mod Menu settings.</summary>
    public sealed class HeelHitSettings
    {
        /// <summary>Radius of the heel zone sphere in meters.</summary>
        public float ZoneRadius;

        /// <summary>How far behind the ankle the zone center sits, in meters.</summary>
        public float ZoneBackOffset;

        /// <summary>How far above the ankle the zone center sits, in meters.</summary>
        public float ZoneHeightOffset;

        /// <summary>Max angle in degrees between "straight behind the target" and the attacker.</summary>
        public float MaxHitAngle;
    }

    /// <summary>One hit, as plain values taken from the game.</summary>
    public sealed class HeelHitInput
    {
        public bool AttackerIsPlayer;
        public bool TargetIsPlayer;

        /// <summary>True if the attacking creature is the hit creature itself.</summary>
        public bool AttackerIsTarget;

        public LegSide Side;
        public HitDamageType DamageType;
        public Vec3 ContactPoint;

        /// <summary>False if the foot bone on the hit side could not be found.</summary>
        public bool HasAnkle;

        public Vec3 Ankle;
        public Vec3 TargetPosition;
        public Vec3 TargetForward;

        /// <summary>False if the attacking creature could not be resolved.</summary>
        public bool HasAttackerPosition;

        public Vec3 AttackerPosition;
    }

    public sealed class HeelHitResult
    {
        public HeelHitRejection Rejection;

        public bool HasZone;
        public Vec3 ZoneCenter;

        /// <summary>Meters from the contact point to the zone center; NaN without a zone.</summary>
        public float DistanceToZoneCenter = float.NaN;

        /// <summary>Degrees between "straight behind the target" and the attacker; NaN if unknown.</summary>
        public float AttackAngle = float.NaN;

        public bool IsHeelHit => Rejection == HeelHitRejection.None;
    }

    public static class HeelZone
    {
        /// <summary>
        /// Center of the heel zone: behind the ankle (relative to the creature's
        /// horizontal facing) and slightly above it, where the Achilles tendon runs.
        /// </summary>
        public static Vec3 Center(Vec3 ankle, Vec3 targetForward, float backOffset, float heightOffset)
        {
            var back = -targetForward.Flattened.Normalized;
            return ankle + back * backOffset + Vec3.Up * heightOffset;
        }
    }

    public static class AttackAngle
    {
        /// <summary>
        /// Horizontal angle in degrees between "straight behind the target" and the
        /// direction from the target to the attacker: 0 = behind, 90 = side,
        /// 180 = front. NaN if either direction has no horizontal component.
        /// </summary>
        public static float FromBehind(Vec3 targetPosition, Vec3 targetForward, Vec3 attackerPosition)
        {
            var behind = -targetForward.Flattened;
            var toAttacker = (attackerPosition - targetPosition).Flattened;
            return Vec3.AngleDegrees(behind, toAttacker);
        }
    }

    public static class DamageTypeFilter
    {
        /// <summary>Only a blade cuts a tendon: slash and pierce, never blunt.</summary>
        public static bool IsAllowed(HitDamageType hit)
        {
            return hit == HitDamageType.Slash || hit == HitDamageType.Pierce;
        }
    }

    public static class HeelHitClassifier
    {
        /// <summary>
        /// Decides whether a hit cuts the Achilles tendon. Distance and angle are
        /// measured whenever the inputs allow it, even if an earlier check already
        /// rejected the hit, so the debug log always shows the numbers for tuning.
        /// </summary>
        public static HeelHitResult Classify(HeelHitInput input, HeelHitSettings settings)
        {
            var result = new HeelHitResult();

            var isLeg = input.Side != LegSide.None;
            if (isLeg && input.HasAnkle)
            {
                result.HasZone = true;
                result.ZoneCenter = HeelZone.Center(
                    input.Ankle, input.TargetForward, settings.ZoneBackOffset, settings.ZoneHeightOffset);
                result.DistanceToZoneCenter = Vec3.Distance(input.ContactPoint, result.ZoneCenter);
            }

            if (input.HasAttackerPosition)
            {
                result.AttackAngle = AttackAngle.FromBehind(
                    input.TargetPosition, input.TargetForward, input.AttackerPosition);
            }

            result.Rejection = FirstRejection(input, settings, result, isLeg);
            return result;
        }

        /// <summary>Only the player hitting an NPC counts (player hardcore mode dropped, Nadi 2026-11-17).</summary>
        private static bool IsPlayerHitsNpc(HeelHitInput input)
        {
            return input.AttackerIsPlayer && !input.TargetIsPlayer;
        }

        private static HeelHitRejection FirstRejection(
            HeelHitInput input, HeelHitSettings settings, HeelHitResult measured, bool isLeg)
        {
            if (input.AttackerIsTarget)
            {
                return HeelHitRejection.SelfHit;
            }
            if (!IsPlayerHitsNpc(input))
            {
                return HeelHitRejection.NotPlayerVsNpc;
            }
            if (!isLeg)
            {
                return HeelHitRejection.NotLegPart;
            }
            if (!input.HasAnkle)
            {
                return HeelHitRejection.NoAnkle;
            }
            if (!DamageTypeFilter.IsAllowed(input.DamageType))
            {
                return HeelHitRejection.DamageTypeNotAllowed;
            }
            if (measured.DistanceToZoneCenter > settings.ZoneRadius)
            {
                return HeelHitRejection.OutsideHeelZone;
            }
            if (float.IsNaN(measured.AttackAngle))
            {
                return HeelHitRejection.AttackAngleUndetermined;
            }
            if (measured.AttackAngle > settings.MaxHitAngle)
            {
                return HeelHitRejection.AttackAngleTooFrontal;
            }
            return HeelHitRejection.None;
        }
    }
}
