using AchillesTendon.Logic;
using ThunderRoad;
using UnityEngine;

namespace AchillesTendon
{
    /// <summary>
    /// Turns ThunderRoad / Unity objects into the plain values the logic works on.
    /// No decisions are made here; those live in <see cref="HeelHitClassifier"/>.
    /// </summary>
    internal static class GameAdapters
    {
        public static Vec3 ToVec3(Vector3 v) => new Vec3(v.x, v.y, v.z);

        public static Vector3 ToVector3(Vec3 v) => new Vector3(v.X, v.Y, v.Z);

        /// <summary>
        /// Stops the game's dying animation of an NPC that died crawling
        /// (BrainModuleDeath.StopDying, the game's own abort, also used when a dying NPC is
        /// pushed): it then falls as a plain ragdoll instead of hanging in a standing death clip.
        /// </summary>
        public static void StopDyingAnimationIfCrawling(Creature creature, bool wasCrawling)
        {
            var brain = creature.brain;
            if (brain == null || !DeathRules.StopDyingAnimation(wasCrawling, brain.isDying))
            {
                return;
            }
            var death = brain.instance != null ? brain.instance.GetModule<BrainModuleDeath>(autoAddModuleIfMissing: false) : null;
            if (death == null)
            {
                return;
            }
            death.StopDying();
            ModLog.Verbose("DEATH | " + Label(creature) + " | died crawling, standing death animation stopped");
        }

        public static LegSide SideOf(RagdollPart part)
        {
            if (part == null)
            {
                return LegSide.None;
            }
            switch (part.type)
            {
                case RagdollPart.Type.LeftLeg:
                case RagdollPart.Type.LeftFoot:
                    return LegSide.Left;
                case RagdollPart.Type.RightLeg:
                case RagdollPart.Type.RightFoot:
                    return LegSide.Right;
                default:
                    return LegSide.None;
            }
        }

        public static HitDamageType ToHitDamageType(DamageType type)
        {
            switch (type)
            {
                case DamageType.Slash:
                    return HitDamageType.Slash;
                case DamageType.Pierce:
                    return HitDamageType.Pierce;
                case DamageType.Blunt:
                    return HitDamageType.Blunt;
                default:
                    return HitDamageType.Other;
            }
        }

        /// <summary>Foot part on the given side; its transform sits at the ankle joint (to verify in game).</summary>
        public static RagdollPart FootOf(Creature creature, LegSide side)
        {
            if (side == LegSide.None || creature == null || creature.ragdoll == null)
            {
                return null;
            }
            var type = side == LegSide.Left ? RagdollPart.Type.LeftFoot : RagdollPart.Type.RightFoot;
            return creature.ragdoll.GetPart(type);
        }

        /// <summary>
        /// The part that was hit: the damage struct's part, else the part behind the
        /// target collider group.
        /// </summary>
        public static RagdollPart HitPart(CollisionInstance hit)
        {
            var part = hit.damageStruct.hitRagdollPart;
            if (part != null)
            {
                return part;
            }
            var group = hit.targetColliderGroup;
            return group != null && group.collisionHandler != null ? group.collisionHandler.ragdollPart : null;
        }

        /// <summary>
        /// Who dealt the hit, resolved the same way Creature.Damage resolves
        /// lastInteractionCreature (held/thrown item's last handler, else spell
        /// caster), plus the creature of a hitting body part (punch, kick).
        /// </summary>
        public static Creature Attacker(CollisionInstance hit)
        {
            var group = hit.sourceColliderGroup;
            var handler = group != null ? group.collisionHandler : null;
            if (handler != null)
            {
                var item = handler.item;
                var hand = item != null ? item.lastHandler : null;
                if (hand != null && hand.creature != null)
                {
                    return hand.creature;
                }
                var part = handler.ragdollPart;
                if (part != null && part.ragdoll != null && part.ragdoll.creature != null)
                {
                    return part.ragdoll.creature;
                }
            }
            var caster = hit.casterHand;
            if (caster != null && caster.mana != null)
            {
                return caster.mana.creature;
            }
            return null;
        }

        /// <summary>The player's focus (Mana.currentFocus), or NaN without a player body; for Debug Logging.</summary>
        public static float PlayerFocus()
        {
            var player = Player.currentCreature;
            if (player == null || player.mana == null)
            {
                return float.NaN;
            }
            return player.mana.currentFocus;
        }

        public static string Label(Creature creature)
        {
            var id = string.IsNullOrEmpty(creature.creatureId) ? creature.name : creature.creatureId;
            return id + " #" + creature.GetInstanceID();
        }

        public static HeelHitInput BuildInput(Creature target, CollisionInstance hit, RagdollPart part, Creature attacker)
        {
            var side = SideOf(part);
            var foot = FootOf(target, side);
            var input = new HeelHitInput
            {
                AttackerIsPlayer = attacker != null && attacker.isPlayer,
                TargetIsPlayer = target.isPlayer,
                AttackerIsTarget = attacker != null && attacker == target,
                Side = side,
                DamageType = ToHitDamageType(hit.damageStruct.damageType),
                ContactPoint = ToVec3(hit.contactPoint),
                HasAnkle = foot != null,
                TargetPosition = ToVec3(target.transform.position),
                TargetForward = ToVec3(target.transform.forward),
                HasAttackerPosition = attacker != null,
            };
            if (foot != null)
            {
                input.Ankle = ToVec3(foot.transform.position);
            }
            if (attacker != null)
            {
                input.AttackerPosition = ToVec3(attacker.transform.position);
            }
            return input;
        }
    }
}
