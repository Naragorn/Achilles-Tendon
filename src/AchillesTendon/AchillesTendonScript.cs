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
    /// (build step 3: it falls and cannot stand up again).
    /// </summary>
    public sealed class AchillesTendonScript : ThunderScript
    {
        private readonly DebugVisualizer visualizer = new DebugVisualizer();
        private readonly CrippleController cripple = new CrippleController();

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            ModLog.Info("Loaded " + modData.Name + " " + modData.ModVersion + " (folder " + modData.folderName + ")");
        }

        public override void ScriptEnable()
        {
            EventManager.onCreatureHit += OnCreatureHit;
            ModLog.Info("Enabled, listening to EventManager.onCreatureHit");
        }

        public override void ScriptDisable()
        {
            EventManager.onCreatureHit -= OnCreatureHit;
            visualizer.Clear();
            cripple.ReleaseAll();
            ModLog.Info("Disabled");
        }

        public override void ScriptUnload()
        {
            EventManager.onCreatureHit -= OnCreatureHit;
            visualizer.Clear();
            cripple.ReleaseAll();
        }

        public override void ScriptUpdate()
        {
            try
            {
                cripple.Update();
            }
            catch (Exception ex)
            {
                ModLog.Error("Cripple update failed: " + ex);
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

        // OnStart is used because Creature.Damage only raises OnEnd for hits that
        // do not kill; a killing heel cut must be seen too.
        private void OnCreatureHit(Creature creature, CollisionInstance hit, EventTime eventTime)
        {
            if (eventTime != EventTime.OnStart || !ModOptions.TendonEnabled)
            {
                return;
            }
            if (creature == null || hit == null || !hit.damageStruct.active || hit.damageStruct.damage <= 0f)
            {
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

        private void HandleHit(Creature creature, CollisionInstance hit)
        {
            var part = GameAdapters.HitPart(hit);
            var attacker = GameAdapters.Attacker(hit);
            var input = GameAdapters.BuildInput(creature, hit, part, attacker);
            var settings = ModOptions.CurrentHeelHitSettings();
            var result = HeelHitClassifier.Classify(input, settings);

            // NPC vs NPC, and NPC hits on the player while Apply To Player is
            // off, are out of scope: stay silent.
            if (result.Rejection == HeelHitRejection.NotPlayerVsNpc)
            {
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
                cripple.Cripple(creature);
            }
        }
    }
}
