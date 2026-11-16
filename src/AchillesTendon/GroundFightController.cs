using System.Collections.Generic;
using AchillesTendon.Logic;
using ThunderRoad;
using UnityEngine;

namespace AchillesTendon
{
    /// <summary>
    /// Build step 4, route A (experimental): a crippled NPC fights on from the ground.
    ///
    /// Engine facts this relies on (ThunderRoad.dll 1.3.2, see README):
    /// - In Ragdoll.State.Destabilized the animator stays enabled and every non-root
    ///   part is pulled toward the animated pose by a rotation spring
    ///   (springRotationForce * destabilizedGroundSpringRotationMultiplier once on the
    ///   ground, Ragdoll.OnFallEvent). Raising that spring on the arm parts lets an
    ///   animation move the arms of a lying body; electrocution animates a lying NPC
    ///   the same way (BrainModuleElectrocute).
    /// - Attack clips come from the NPC's own melee stance (BrainModuleMelee.meleeIdle,
    ///   only set while it holds a weapon in a melee stance). They are captured when the
    ///   tendon is cut, because the stance may drop its pose once the NPC lies down.
    /// - Creature.PlayAnimation(clip, loop:false) is the call BrainModuleMelee uses for
    ///   its own attacks.
    /// - Screams: BrainModuleSpeak.Play("hit"), the voice's pain dialog.
    /// Whether the animator actually lets the swing through while the NPC lies down is
    /// the open question this step tests.
    /// </summary>
    internal sealed class GroundFightController
    {
        private const RagdollPart.Type ArmParts =
            RagdollPart.Type.LeftArm | RagdollPart.Type.RightArm | RagdollPart.Type.LeftHand | RagdollPart.Type.RightHand;

        private const string ScreamDialogId = "hit";

        private sealed class Fighter
        {
            public readonly List<AnimationClip> AttackClips = new List<AnimationClip>();
            public float LastAttackTime = float.NegativeInfinity;
            public float LastScreamTime = float.NegativeInfinity;
            public bool ArmsStrong;
            public float AppliedArmStrength;
            public bool NoClipLogged;

            /// <summary>We started a swing; it is stopped when the NPC leaves ground fighting.</summary>
            public bool StartedAnimation;
        }

        private readonly Dictionary<Creature, Fighter> fighters = new Dictionary<Creature, Fighter>();
        private readonly List<Creature> scratch = new List<Creature>();

        /// <summary>Called when the tendon is cut, while the NPC still stands in its stance.</summary>
        public void Start(Creature creature)
        {
            if (fighters.ContainsKey(creature))
            {
                return;
            }
            var fighter = new Fighter();
            CaptureAttackClips(creature, fighter.AttackClips);
            fighters[creature] = fighter;
            ModLog.Verbose("GROUND FIGHT START | " + GameAdapters.Label(creature) + " | attack clips=" + fighter.AttackClips.Count);
        }

        public void Update(float time, System.Func<Creature, bool> isCrippled, System.Func<Creature, bool> isDowned, System.Func<Creature, bool> isCrawling)
        {
            if (fighters.Count == 0)
            {
                return;
            }
            scratch.Clear();
            scratch.AddRange(fighters.Keys);
            foreach (var creature in scratch)
            {
                var fighter = fighters[creature];
                if (creature == null || !Creature.allActive.Contains(creature) || creature.isKilled || !isCrippled(creature))
                {
                    if (creature != null && !creature.isKilled)
                    {
                        Relax(creature, fighter);
                    }
                    fighters.Remove(creature);
                    continue;
                }

                var decision = GroundFightRules.Decide(new GroundFightInput
                {
                    FeatureOn = ModOptions.TendonEnabled && ModOptions.GroundFightingEnabled,
                    IsDown = isDowned(creature),
                    IsCrawling = isCrawling(creature),
                    AttacksOn = ModOptions.GroundAttacks,
                    HasAttackClip = fighter.AttackClips.Count > 0,
                    IsPlayingAnimation = creature.isPlayingDynamicAnimation,
                    DistanceToPlayer = DistanceToPlayer(creature),
                    AttackRange = ModOptions.GroundAttackRange,
                    SinceLastAttack = time - fighter.LastAttackTime,
                    AttackInterval = ModOptions.GroundAttackInterval,
                    ScreamsOn = ModOptions.Screaming,
                    SinceLastScream = time - fighter.LastScreamTime,
                    ScreamInterval = ModOptions.ScreamInterval,
                });

                ApplyArms(creature, fighter, decision.StrongArms);
                if (!decision.StrongArms && fighter.StartedAnimation)
                {
                    StopSwing(creature, fighter);
                }
                if (decision.Attack)
                {
                    fighter.LastAttackTime = time;
                    Attack(creature, fighter, decision.UpperBodyOnly);
                }
                if (decision.Scream)
                {
                    fighter.LastScreamTime = time;
                    Scream(creature);
                }
                if (ModOptions.GroundAttacks && fighter.AttackClips.Count == 0 && !fighter.NoClipLogged)
                {
                    fighter.NoClipLogged = true;
                    ModLog.Verbose("GROUND ATTACK skipped | " + GameAdapters.Label(creature) + " | no attack animations (no weapon in a melee stance when cut)");
                }
            }
        }

        /// <summary>Gives every arm back the game's own lying value and stops tracking.</summary>
        public void Clear()
        {
            foreach (var pair in fighters)
            {
                if (pair.Key != null && !pair.Key.isKilled)
                {
                    Relax(pair.Key, pair.Value);
                }
            }
            fighters.Clear();
        }

        // Re-applied every frame while strong: the game resets the pins on every state
        // change and when the body lands (Ragdoll.OnFallEvent).
        private static void ApplyArms(Creature creature, Fighter fighter, bool strong)
        {
            if (strong)
            {
                SetArmStrength(creature, ModOptions.ArmStrength);
                if (!fighter.ArmsStrong || fighter.AppliedArmStrength != ModOptions.ArmStrength)
                {
                    ModLog.Verbose("ARMS | " + GameAdapters.Label(creature) + " | strength x" + ModOptions.ArmStrength);
                }
                fighter.ArmsStrong = true;
                fighter.AppliedArmStrength = ModOptions.ArmStrength;
            }
            else if (fighter.ArmsStrong)
            {
                SetArmStrength(creature, 1f);
                fighter.ArmsStrong = false;
                ModLog.Verbose("ARMS | " + GameAdapters.Label(creature) + " | back to the game's lying value");
            }
        }

        private static void Relax(Creature creature, Fighter fighter)
        {
            if (fighter.ArmsStrong)
            {
                SetArmStrength(creature, 1f);
                fighter.ArmsStrong = false;
            }
            if (fighter.StartedAnimation)
            {
                StopSwing(creature, fighter);
            }
        }

        // Headless test (harness/step4-a): a swing started on a lying NPC never reports its
        // end, so isPlayingDynamicAnimation stays set. Stopping it keeps the NPC from playing
        // the swing once it stands up again.
        private static void StopSwing(Creature creature, Fighter fighter)
        {
            if (creature.isPlayingDynamicAnimation)
            {
                creature.StopAnimation(interrupt: true);
            }
            fighter.StartedAnimation = false;
        }

        private static void SetArmStrength(Creature creature, float multiplier)
        {
            var ragdoll = creature.ragdoll;
            var spring = ragdoll.springRotationForce * ragdoll.destabilizedGroundSpringRotationMultiplier * multiplier;
            var damper = ragdoll.damperRotationForce * ragdoll.destabilizedDamperRotationMultiplier;
            var maxForce = ragdoll.maxRotationForce * multiplier;
            foreach (var part in ragdoll.parts)
            {
                if (part != ragdoll.rootPart && (part.type & ArmParts) != 0)
                {
                    part.bone.SetPinRotationForce(spring, damper, maxForce);
                }
            }
        }

        private static void Attack(Creature creature, Fighter fighter, bool upperBodyOnly)
        {
            var clip = fighter.AttackClips[Random.Range(0, fighter.AttackClips.Count)];
            if (upperBodyOnly)
            {
                // The upper-body layer the game uses e.g. for reaching for a door while standing
                // (BrainModuleMove); it does not set isPlayingDynamicAnimation.
                creature.PlayUpperAnimation(clip, loop: false);
            }
            else
            {
                creature.PlayAnimation(clip, loop: false);
                fighter.StartedAnimation = true;
            }
            ModLog.Verbose("GROUND ATTACK | " + GameAdapters.Label(creature) + " | " + (upperBodyOnly ? "upper body" : "full body") + " | clip=" + clip.name
                + " | distance=" + DistanceToPlayer(creature).ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture) + " m");
        }

        private static void Scream(Creature creature)
        {
            var speak = creature.brain != null && creature.brain.instance != null
                ? creature.brain.instance.GetModule<BrainModuleSpeak>(autoAddModuleIfMissing: false)
                : null;
            if (speak == null)
            {
                return;
            }
            speak.Play(ScreamDialogId, abnormalNoise: true);
            ModLog.Verbose("SCREAM | " + GameAdapters.Label(creature));
        }

        private static float DistanceToPlayer(Creature creature)
        {
            var player = Player.local != null ? Player.local.creature : null;
            if (player == null)
            {
                return float.PositiveInfinity;
            }
            var from = creature.ragdoll.rootPart != null ? creature.ragdoll.rootPart.transform.position : creature.transform.position;
            return Vector3.Distance(from, player.transform.position);
        }

        // First choice: the melee pose the NPC stands in right now. An NPC without a
        // target (e.g. HumanDummy) holds its weapon in a plain idle pose, so the fallback
        // is the stance its equipment maps to (BrainModuleStance.GetStanceDataFromEquipment,
        // the lookup the game uses when combat starts); its clips are loaded with the catalog.
        private static void CaptureAttackClips(Creature creature, List<AnimationClip> clips)
        {
            var instance = creature.brain != null ? creature.brain.instance : null;
            if (instance == null)
            {
                return;
            }
            var melee = instance.GetModule<BrainModuleMelee>(autoAddModuleIfMissing: false);
            if (melee != null && melee.meleeIdle != null && melee.meleeIdle.attackMotions != null)
            {
                foreach (var motion in melee.meleeIdle.attackMotions)
                {
                    AddClip(motion, clips);
                }
            }
            if (clips.Count > 0)
            {
                return;
            }
            var stance = instance.GetModule<BrainModuleStance>(autoAddModuleIfMissing: false);
            var stanceData = stance != null ? stance.currentStanceData ?? stance.GetStanceDataFromEquipment() : null;
            if (stanceData == null)
            {
                return;
            }
            foreach (var node in stanceData.AllStanceNodes())
            {
                AddClip(node as AttackMotion, clips);
            }
        }

        private static void AddClip(AttackMotion motion, List<AnimationClip> clips)
        {
            if (motion != null && motion.animationClip != null && !clips.Contains(motion.animationClip))
            {
                clips.Add(motion.animationClip);
            }
        }
    }
}
