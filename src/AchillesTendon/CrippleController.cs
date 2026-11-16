using System.Collections.Generic;
using AchillesTendon.Logic;
using ThunderRoad;

namespace AchillesTendon
{
    /// <summary>
    /// Build step 3: a cut tendon takes the NPC down for good, either into the game's
    /// own injured/crawl animations (Downed Pose = Crawl) or flat on the ground (Lie Flat).
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
        private readonly HashSet<Creature> crawling = new HashSet<Creature>();

        // Crawling NPCs whose own melee AI we switched off, with the value to restore.
        private readonly Dictionary<Creature, bool> meleeBefore = new Dictionary<Creature, bool>();
        private readonly List<Creature> scratch = new List<Creature>();

        public bool IsCrippled(Creature creature) => crippled.Contains(creature);

        public void Cripple(Creature creature)
        {
            crippled.Add(creature);
            if (ModOptions.DownedPose == DownedPose.Crawl)
            {
                Crawl(creature);
                ModLog.Verbose("COLLAPSE | " + GameAdapters.Label(creature) + " | tendon cut, down to the injured/crawl pose");
            }
            else
            {
                Collapse(creature);
                ModLog.Verbose("COLLAPSE | " + GameAdapters.Label(creature) + " | tendon cut, held on the ground");
            }
        }

        /// <summary>Crippled and in the crawl pose (a standing ragdoll kept low).</summary>
        public bool IsCrawling(Creature creature) => crawling.Contains(creature);

        /// <summary>Crippled and either crawling or lying on the ground.</summary>
        public bool IsDowned(Creature creature) => crawling.Contains(creature) || (crippled.Contains(creature) && StateOf(creature).IsDown);

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
                var action = CrippleRules.Decide(StateOf(creature), ModOptions.TendonEnabled, ModOptions.StandUpWhenDisabled, ModOptions.DownedPose);
                switch (action)
                {
                    case CrippleAction.HoldCrawl:
                        Crawl(creature);
                        break;
                    case CrippleAction.HoldDown:
                        StopCrawl(creature);
                        Hold(creature);
                        break;
                    case CrippleAction.Collapse:
                        StopCrawl(creature);
                        Collapse(creature);
                        ModLog.Verbose("RE-COLLAPSE | " + GameAdapters.Label(creature) + " | was getting up while crippled");
                        break;
                    case CrippleAction.Release:
                        StopCrawl(creature);
                        Release(creature);
                        crippled.Remove(creature);
                        ModLog.Verbose("RELEASE | " + GameAdapters.Label(creature) + " | feature off, may stand up");
                        break;
                    case CrippleAction.Forget:
                        if (creature != null)
                        {
                            StopCrawl(creature);
                        }
                        crawling.Remove(creature);
                        meleeBefore.Remove(creature);
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
                    StopCrawl(creature);
                    Release(creature);
                }
            }
            crippled.Clear();
            crawling.Clear();
            meleeBefore.Clear();
        }

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

        // Crawl pose, the same animator settings Burning uses for a burning NPC that drops:
        // a low Height and Injured = 1 blend the base layer's idle states into
        // Laying/LayingInjured and its moving states into Crawling/CrawlBackwards
        // (Human animator controller, read from animations_assets_all.bundle). The body
        // stays a standing ragdoll, so the AI keeps moving (slowly) and attacking.
        // Re-applied every frame: the game resets Height on state changes and while blocking.
        private void Crawl(Creature creature)
        {
            if (creature.brain != null)
            {
                creature.brain.RemoveNoStandUpModifier(this);
            }
            creature.SetAnimatorHeightRatio(ModOptions.CrawlBodyHeight);
            creature.animator.SetFloat(Brain.hashInjured, 1f);
            creature.locomotion.SetAllSpeedModifiers(this, ModOptions.CrawlSpeed);
            creature.ragdoll.forcePhysic.Add(this);
            crawling.Add(creature);
            // The AI's own attack clips move the whole body and raise a crawling NPC
            // upright (headless pictures), so its melee is off while crawling; swings come
            // from GroundFightController on the upper-body layer (Ground Attacks).
            var melee = Melee(creature);
            if (melee != null && !meleeBefore.ContainsKey(creature))
            {
                meleeBefore[creature] = melee.meleeEnabled;
            }
            if (melee != null)
            {
                melee.meleeEnabled = false;
            }
        }

        private static BrainModuleMelee Melee(Creature creature)
        {
            return creature.brain != null && creature.brain.instance != null
                ? creature.brain.instance.GetModule<BrainModuleMelee>(autoAddModuleIfMissing: false)
                : null;
        }

        private void StopCrawl(Creature creature)
        {
            if (!crawling.Remove(creature))
            {
                return;
            }
            creature.SetAnimatorHeightRatio(1f);
            creature.animator.SetFloat(Brain.hashInjured, 0f);
            creature.locomotion.RemoveSpeedModifier(this);
            creature.ragdoll.forcePhysic.Remove(this);
            if (meleeBefore.TryGetValue(creature, out var enabled))
            {
                var melee = Melee(creature);
                if (melee != null)
                {
                    melee.meleeEnabled = enabled;
                }
                meleeBefore.Remove(creature);
            }
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
