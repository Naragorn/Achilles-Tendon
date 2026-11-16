using System.Collections.Generic;
using AchillesTendon.Logic;
using ThunderRoad;

namespace AchillesTendon
{
    /// <summary>
    /// Build step 3: a cut tendon knocks the NPC down and keeps it from standing up.
    ///
    /// Engine facts this relies on (ThunderRoad.dll 1.3.2, see README):
    /// - Knock-down: the same calls as Creature.PushFallback, CancelGetUp(false)
    ///   then Ragdoll.SetState(Destabilized).
    /// - Stand-up: only BrainModuleHitReaction.OnCycle calls Ragdoll.StandUp, and
    ///   only while Brain.noStandupModifiers is empty. Holding a modifier keeps the
    ///   NPC down; removing it lets the game stand it up by itself.
    /// - Brain.Stop clears all modifiers (death, despawn, and catching fire), so the
    ///   hold is re-applied every frame instead of once.
    /// </summary>
    internal sealed class CrippleController
    {
        private readonly HashSet<Creature> crippled = new HashSet<Creature>();
        private readonly List<Creature> scratch = new List<Creature>();

        public bool IsCrippled(Creature creature) => crippled.Contains(creature);

        public void Cripple(Creature creature)
        {
            crippled.Add(creature);
            Collapse(creature);
            ModLog.Verbose("COLLAPSE | " + GameAdapters.Label(creature) + " | tendon cut, held on the ground");
        }

        public void Update()
        {
            if (crippled.Count == 0)
            {
                return;
            }
            scratch.Clear();
            scratch.AddRange(crippled);
            foreach (var creature in scratch)
            {
                var action = CrippleRules.Decide(StateOf(creature), ModOptions.TendonEnabled, ModOptions.StandUpWhenDisabled);
                switch (action)
                {
                    case CrippleAction.HoldDown:
                        Hold(creature);
                        break;
                    case CrippleAction.Collapse:
                        Collapse(creature);
                        ModLog.Verbose("RE-COLLAPSE | " + GameAdapters.Label(creature) + " | was getting up while crippled");
                        break;
                    case CrippleAction.Release:
                        Release(creature);
                        crippled.Remove(creature);
                        ModLog.Verbose("RELEASE | " + GameAdapters.Label(creature) + " | feature off, may stand up");
                        break;
                    case CrippleAction.Forget:
                        crippled.Remove(creature);
                        break;
                }
            }
        }

        /// <summary>Lets every crippled NPC stand up again (mod disabled or unloaded).</summary>
        public void ReleaseAll()
        {
            foreach (var creature in crippled)
            {
                if (creature != null && !creature.isKilled)
                {
                    Release(creature);
                }
            }
            crippled.Clear();
        }

        /// <summary>Lying on the ground and not in the middle of getting up.</summary>
        public bool IsDown(Creature creature) => StateOf(creature).IsDown;

        private static CrippledCreatureState StateOf(Creature creature)
        {
            if (creature == null || !Creature.allActive.Contains(creature))
            {
                return new CrippledCreatureState { Exists = false };
            }
            return new CrippledCreatureState
            {
                Exists = true,
                IsKilled = creature.isKilled,
                IsDown = creature.state == Creature.State.Destabilized && !creature.ragdoll.standingUp,
            };
        }

        private void Collapse(Creature creature)
        {
            if (creature.ragdoll.standingUp)
            {
                creature.ragdoll.CancelGetUp(resetState: false);
            }
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
            Hold(creature);
        }

        // The controller instance is the modifier key, so only its own hold is removed.
        // Explicit != null: ?. would skip Unity's destroyed-object check.
        private void Hold(Creature creature)
        {
            if (creature.brain != null)
            {
                creature.brain.AddNoStandUpModifier(this);
            }
        }

        private void Release(Creature creature)
        {
            if (creature.brain != null)
            {
                creature.brain.RemoveNoStandUpModifier(this);
            }
        }
    }
}
