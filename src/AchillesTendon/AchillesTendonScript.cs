using System;
using AchillesTendon.Logic;
using ThunderRoad;

namespace AchillesTendon
{
    /// <summary>
    /// Entry point. The game's ModManager creates one instance of every
    /// ThunderScript subclass in a mod DLL and calls ScriptLoaded, then Enable.
    ///
    /// Detects heel hits, reports them (log + markers) and cripples the NPC
    /// (build step 3: it falls and cannot stand up again), then it bleeds out
    /// (build step 5).
    /// </summary>
    public sealed class AchillesTendonScript : ThunderScript
    {
        private readonly DebugVisualizer visualizer = new DebugVisualizer();
        private readonly CrippleController cripple = new CrippleController();
        private readonly BleedController bleed = new BleedController();
        private readonly GroundFightController groundFight = new GroundFightController();
        private readonly DmcsAdrenaline dmcs = new DmcsAdrenaline();
        private readonly CollapseCry cry = new CollapseCry();

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            ModLog.Info("Loaded " + modData.Name + " " + modData.ModVersion + " (folder " + modData.folderName + ")");
        }

        public override void ScriptEnable()
        {
            EventManager.onCreatureHit += OnCreatureHit;
            EventManager.onCreatureKill += OnCreatureKill;
            ModLog.Info("Enabled, listening to EventManager.onCreatureHit");
        }

        public override void ScriptDisable()
        {
            EventManager.onCreatureHit -= OnCreatureHit;
            EventManager.onCreatureKill -= OnCreatureKill;
            dmcs.Clear();
            visualizer.Clear();
            cripple.ReleaseAll();
            bleed.Clear();
            groundFight.Clear();
            cry.Clear();
            ModLog.Info("Disabled");
        }

        public override void ScriptUnload()
        {
            EventManager.onCreatureHit -= OnCreatureHit;
            EventManager.onCreatureKill -= OnCreatureKill;
            dmcs.Clear();
            visualizer.Clear();
            cripple.ReleaseAll();
            bleed.Clear();
            groundFight.Clear();
            cry.Clear();
        }

        public override void ScriptUpdate()
        {
            try
            {
                cripple.Update(c => WeakeningOf(c).CrawlSpeedFactor);
            }
            catch (Exception ex)
            {
                ModLog.Error("Cripple update failed: " + ex);
            }
            try
            {
                bleed.Update(UnityEngine.Time.deltaTime, cripple.IsCrippled, WeakeningOf);
            }
            catch (Exception ex)
            {
                ModLog.Error("Bleed update failed: " + ex);
            }
            try
            {
                groundFight.Update(UnityEngine.Time.time, cripple.IsCrippled, cripple.IsDowned, cripple.IsCrawling, WeakeningOf);
            }
            catch (Exception ex)
            {
                ModLog.Error("Ground fight update failed: " + ex);
            }
            try
            {
                cry.Update(UnityEngine.Time.time);
            }
            catch (Exception ex)
            {
                ModLog.Error("Collapse cry update failed: " + ex);
            }
            try
            {
                visualizer.Update();
            }
            catch (Exception ex)
            {
                ModLog.Error("Debug visualizer failed: " + ex);
                visualizer.Clear();
            }
        }

        // Build step 5, "weaker over time": derived from the bleed-out progress each frame,
        // so changing the menu or pausing the bleed-out applies at once.
        private Weakening WeakeningOf(Creature creature)
        {
            var isBleeding = bleed.TryGetProgress(creature, out var progress);
            return WeakeningRules.Compute(new WeakeningInput
            {
                FeatureOn = ModOptions.Weakening,
                IsBleeding = isBleeding,
                Progress = progress,
                WeakestCrawlSpeed = ModOptions.WeakestCrawlSpeed,
                ScreamSlowdown = ModOptions.ScreamSlowdown,
                GaspingFrom = ModOptions.GaspingFrom,
            });
        }

        // OnStart is used because Creature.Damage only raises OnEnd for hits that
        // do not kill; a killing heel cut must be seen too.
        private void OnCreatureHit(Creature creature, CollisionInstance hit, EventTime eventTime)
        {
            if (eventTime != EventTime.OnStart || !ModOptions.TendonEnabled)
            {
                return;
            }
            if (creature == null || hit == null)
            {
                return;
            }
            if (!hit.damageStruct.active || hit.damageStruct.damage <= 0f)
            {
                ModLog.Verbose("skip (inactive or no damage) | target=" + GameAdapters.Label(creature)
                    + " | active=" + hit.damageStruct.active + " damage=" + hit.damageStruct.damage);
                return;
            }

            try
            {
                HandleHit(creature, hit);
            }
            catch (Exception ex)
            {
                ModLog.Error("Heel hit check failed: " + ex);
            }
        }

        // OnStart: the creature is still tracked as crippled (CrippleController forgets the
        // dead in its next update). Creature.Kill raises OnStart only when the kill goes ahead.
        private void OnCreatureKill(Creature creature, Player player, CollisionInstance hit, EventTime eventTime)
        {
            if (creature == null)
            {
                return;
            }
            if (eventTime == EventTime.OnEnd)
            {
                // Runs after BrainModuleDeath.OnKill (Creature.Kill raises OnKillEvent before
                // EventManager.onCreatureKill), so its dying animation has already started.
                try
                {
                    GameAdapters.StopDyingAnimationIfCrawling(creature, cripple.IsCrawling(creature));
                }
                catch (Exception ex)
                {
                    ModLog.Error("Death animation check failed: " + ex);
                }
                return;
            }
            if (eventTime != EventTime.OnStart)
            {
                return;
            }
            try
            {
                dmcs.OnKill(creature, hit, cripple.IsCrippled(creature));
            }
            catch (Exception ex)
            {
                ModLog.Error("DMCS kill check failed: " + ex);
            }
        }

        private void HandleHit(Creature creature, CollisionInstance hit)
        {
            var part = GameAdapters.HitPart(hit);
            var attacker = GameAdapters.Attacker(hit);
            var input = GameAdapters.BuildInput(creature, hit, part, attacker);
            var settings = ModOptions.CurrentHeelHitSettings();
            var result = HeelHitClassifier.Classify(input, settings);

            // A creature hitting itself (a lying NPC touching its own body with its
            // weapon) is frequent and never relevant: dropped without a log line.
            if (result.Rejection == HeelHitRejection.SelfHit)
            {
                return;
            }

            // NPC vs NPC and hits on the player are out of scope: only a
            // one-line note with Debug Logging.
            if (result.Rejection == HeelHitRejection.NotPlayerVsNpc)
            {
                ModLog.Verbose("skip (NotPlayerVsNpc) | target=" + GameAdapters.Label(creature)
                    + " | attacker=" + (attacker != null ? GameAdapters.Label(attacker) : "unknown"));
                return;
            }

            if (ModOptions.DebugLogging)
            {
                ModLog.Info(HitLogFormatter.Format(new HitLogEntry
                {
                    Target = GameAdapters.Label(creature),
                    PartName = part != null ? part.name : "none",
                    PartType = part != null ? part.type.ToString() : "none",
                    PartSection = part != null ? part.section.ToString() : "none",
                    DamageType = hit.damageStruct.damageType.ToString(),
                    Damage = hit.damageStruct.damage,
                    ImpactVelocityAngle = Vec3.AngleDegrees(
                        input.TargetForward.Flattened, GameAdapters.ToVec3(hit.impactVelocity).Flattened),
                    Result = result,
                    ZoneRadius = settings.ZoneRadius,
                    MaxHitAngle = settings.MaxHitAngle,
                }));
            }

            if (input.Side != LegSide.None)
            {
                visualizer.AddHitMarker(hit.contactPoint, result.IsHeelHit);
            }

            cripple.WatchTurns(creature);

            if (CrippleRules.ShouldCripple(result, creature.isPlayer, creature.isKilled, cripple.IsCrippled(creature)))
            {
                if (TryKillAtOnce(creature, hit))
                {
                    return;
                }
                // Captured before the collapse: the attack clips come from the standing stance.
                groundFight.Start(creature, UnityEngine.Time.time);
                cripple.Cripple(creature);
                cry.Play(creature, UnityEngine.Time.time);
                // Tracked even with Bleed-Out off: it is paused then and starts when turned on.
                bleed.Start(creature, part, hit.contactPoint);
                dmcs.OnCrippled(creature, hit);
            }
        }

        // Instant Death Chance (Nadi, 2026-11-20). The cutting hit itself kills, through the game's own
        // Creature.Kill(hit), as DMCS's critical does. This runs at the hit's start, before the game's damage,
        // while the NPC still stands: Kill raises the kill events with this player hit (the Combat Focus
        // skill's focus, DMCS, BrainModuleDeath's dying animation picked from the standing pose and the cut's
        // direction), and Creature.Damage then sees isKilled and only raises the hit's end. Not crippled first:
        // the crawl pose would turn the dying animation into a ragdoll (DeathRules.StopDyingAnimation).
        private bool TryKillAtOnce(Creature creature, CollisionInstance hit)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var roll = UnityEngine.Random.value;
            var rolled = " (roll " + roll.ToString("0.00", ci)
                + ", instant death chance " + OptionSpecs.InstantDeathChance.Label(ModOptions.InstantDeathChance) + ")";
            if (CrippleRules.OutcomeFor(ModOptions.InstantDeathChance, roll) != CutOutcome.InstantDeath)
            {
                return false;
            }
            var focusBefore = GameAdapters.PlayerFocus();
            creature.Kill(hit);
            if (!creature.isKilled)
            {
                ModLog.Verbose("INSTANT DEATH refused | " + GameAdapters.Label(creature) + " | the game kept it alive" + rolled + ", crippled instead");
                return false;
            }
            ModLog.Verbose("INSTANT DEATH | " + GameAdapters.Label(creature) + " | tendon cut, killed at once" + rolled
                + " | dying animation " + (creature.brain != null && creature.brain.isDying ? "yes" : "no (ragdoll)")
                + " | player focus " + focusBefore.ToString("0.0", ci) + " -> " + GameAdapters.PlayerFocus().ToString("0.0", ci));
            dmcs.OnKilledByCut(creature);
            return true;
        }
    }
}
