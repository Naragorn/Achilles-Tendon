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

            if (CrippleRules.ShouldCripple(result, creature.isPlayer, creature.isKilled, cripple.IsCrippled(creature)))
            {
                // Captured before the collapse: the attack clips come from the standing stance.
                groundFight.Start(creature);
                cripple.Cripple(creature);
                // Tracked even with Bleed-Out off: it is paused then and starts when turned on.
                bleed.Start(creature, part, hit.contactPoint);
                dmcs.OnCrippled(creature, hit);
            }
        }
    }
}
