using System.Collections.Generic;
using AchillesTendon.Logic;
using ThunderRoad;
using UnityEngine;

namespace AchillesTendon
{
    /// <summary>
    /// Build step 4: a crippled NPC fights on from the ground.
    ///
    /// - Screams: BrainModuleSpeak.Play("hit"), the voice's pain dialog; near the end
    ///   of the bleed-out "heavybreath".
    /// - Swings (crawling NPCs only, rolled per NPC from Ground Attack Chance): one of
    ///   its own attack clips on the upper-body layer. Clips come from the NPC's melee
    ///   stance (BrainModuleMelee.meleeIdle, else the stance its equipment maps to),
    ///   captured when the tendon is cut; an NPC cut before it stood in a melee stance
    ///   has none then, so it looks again whenever a swing is due (2026-11-18).
    /// - NPCs lying flat do not swing: a full-body clip on a Destabilized body did not
    ///   move it (harness step4-a, 2026-11-16), so that route and Arm Strength were
    ///   removed (Nadi, 2026-11-17).
    /// </summary>
    internal sealed class GroundFightController
    {
        private const string ScreamDialogId = "hit";

        // Every human voice in the catalog (Voices/Voice_Human*.json, 1.3.2) has it.
        private const string GaspDialogId = "heavybreath";

        private sealed class Fighter
        {
            public readonly List<AnimationClip> AttackClips = new List<AnimationClip>();
            public float LastAttackTime = float.NegativeInfinity;
            public float LastScreamTime = float.NegativeInfinity;

            /// <summary>Ground Attack Chance, rolled when the tendon was cut.</summary>
            public bool IsAttacker;
        }

        private readonly Dictionary<Creature, Fighter> fighters = new Dictionary<Creature, Fighter>();
        private readonly List<Creature> scratch = new List<Creature>();

        /// <summary>Called when the tendon is cut (the NPC may not stand in a melee stance yet).</summary>
        public void Start(Creature creature)
        {
            if (fighters.ContainsKey(creature))
            {
                return;
            }
            var fighter = new Fighter();
            var source = CaptureAttackClips(creature, fighter.AttackClips);
            var roll = Random.value;
            fighter.IsAttacker = GroundFightRules.IsAttacker(ModOptions.GroundAttackChance, roll);
            fighters[creature] = fighter;
            ModLog.Verbose("GROUND FIGHT START | " + GameAdapters.Label(creature) + " | attack clips=" + fighter.AttackClips.Count + " (" + source + ")"
                + " | attacker=" + (fighter.IsAttacker ? "yes" : "no")
                + " (roll " + roll.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + ", chance " + OptionSpecs.GroundAttackChance.Label(ModOptions.GroundAttackChance) + ")");
        }

        public void Update(float time, System.Func<Creature, bool> isCrippled, System.Func<Creature, bool> isDowned, System.Func<Creature, bool> isCrawling, System.Func<Creature, Weakening> weakening)
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
                    fighters.Remove(creature);
                    continue;
                }

                var weak = weakening(creature);
                var decision = GroundFightRules.Decide(new GroundFightInput
                {
                    FeatureOn = ModOptions.TendonEnabled && ModOptions.GroundFightingEnabled,
                    IsDown = isDowned(creature),
                    IsCrawling = isCrawling(creature),
                    AttacksOn = ModOptions.GroundAttacks,
                    IsAttacker = fighter.IsAttacker,
                    HasAttackClip = fighter.AttackClips.Count > 0,
                    IsPlayingAnimation = creature.isPlayingDynamicAnimation,
                    DistanceToPlayer = DistanceToPlayer(creature),
                    AttackRange = ModOptions.GroundAttackRange,
                    SinceLastAttack = time - fighter.LastAttackTime,
                    AttackInterval = ModOptions.GroundAttackInterval,
                    ScreamsOn = ModOptions.Screaming,
                    SinceLastScream = time - fighter.LastScreamTime,
                    ScreamInterval = ModOptions.ScreamInterval * weak.ScreamIntervalFactor,
                });

                if (decision.LookForClips)
                {
                    // Cut before it stood in a melee stance: the clips can be there now.
                    var source = CaptureAttackClips(creature, fighter.AttackClips);
                    ModLog.Verbose("GROUND ATTACK clips | " + GameAdapters.Label(creature) + " | looked again when a swing was due: "
                        + fighter.AttackClips.Count + " (" + source + ")");
                    if (fighter.AttackClips.Count == 0)
                    {
                        // Next look after one Ground Attack Interval.
                        fighter.LastAttackTime = time;
                    }
                }
                if (decision.Attack || (decision.LookForClips && fighter.AttackClips.Count > 0))
                {
                    fighter.LastAttackTime = time;
                    Attack(creature, fighter);
                }
                if (decision.Scream)
                {
                    fighter.LastScreamTime = time;
                    Scream(creature, weak.Gasping);
                }
            }
        }

        public void Clear()
        {
            fighters.Clear();
        }

        // The upper-body layer the game uses e.g. for reaching for a door while standing
        // (BrainModuleMove); a full-body clip would raise the crawler upright. It does not
        // set isPlayingDynamicAnimation.
        private static void Attack(Creature creature, Fighter fighter)
        {
            var clip = fighter.AttackClips[Random.Range(0, fighter.AttackClips.Count)];
            creature.PlayUpperAnimation(clip, loop: false);
            ModLog.Verbose("GROUND ATTACK | " + GameAdapters.Label(creature) + " | upper body | clip=" + clip.name
                + " | distance=" + DistanceToPlayer(creature).ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture) + " m");
        }

        private static void Scream(Creature creature, bool gasping)
        {
            var speak = creature.brain != null && creature.brain.instance != null
                ? creature.brain.instance.GetModule<BrainModuleSpeak>(autoAddModuleIfMissing: false)
                : null;
            if (speak == null)
            {
                return;
            }
            speak.Play(gasping ? GaspDialogId : ScreamDialogId, abnormalNoise: true);
            ModLog.Verbose((gasping ? "GASP | " : "SCREAM | ") + GameAdapters.Label(creature));
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
        // the lookup the game uses when combat starts). Returns where the clips came from,
        // or why there are none (Debug Logging).
        private static string CaptureAttackClips(Creature creature, List<AnimationClip> clips)
        {
            var instance = creature.brain != null ? creature.brain.instance : null;
            if (instance == null)
            {
                return "no brain";
            }
            var melee = instance.GetModule<BrainModuleMelee>(autoAddModuleIfMissing: false);
            var meleeMotions = 0;
            if (melee != null && melee.meleeIdle != null && melee.meleeIdle.attackMotions != null)
            {
                foreach (var motion in melee.meleeIdle.attackMotions)
                {
                    meleeMotions++;
                    AddClip(motion, clips);
                }
            }
            if (clips.Count > 0)
            {
                return "melee idle " + melee.meleeIdle.id;
            }
            var report = "melee idle " + (melee == null ? "no module" : melee.meleeIdle == null ? "none" : meleeMotions + " motions, no clip");
            var stance = instance.GetModule<BrainModuleStance>(autoAddModuleIfMissing: false);
            if (stance == null)
            {
                return report + "; no stance module";
            }
            report += "; stance " + (stance.currentStanceData != null ? stance.currentStanceData.id : "none");
            report += Collect(stance.currentStanceData, clips);
            if (clips.Count > 0)
            {
                return report;
            }
            var fromEquipment = stance.GetStanceDataFromEquipment();
            report += "; equipment stance " + (fromEquipment != null ? fromEquipment.id : "none");
            return report + Collect(fromEquipment, clips);
        }

        private static string Collect(StanceData stanceData, List<AnimationClip> clips)
        {
            if (stanceData == null)
            {
                return "";
            }
            int motions = 0, missing = 0;
            foreach (var node in stanceData.AllStanceNodes())
            {
                var motion = node as AttackMotion;
                if (motion == null)
                {
                    continue;
                }
                motions++;
                if (motion.animationClip == null)
                {
                    missing++;
                }
                AddClip(motion, clips);
            }
            return " (" + motions + " attacks, " + missing + " without clip)";
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
