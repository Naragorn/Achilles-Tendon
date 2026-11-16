using System.Collections.Generic;
using System.Globalization;
using AchillesTendon.Logic;
using ThunderRoad;
using UnityEngine;

namespace AchillesTendon
{
    /// <summary>
    /// Build step 5: a crippled NPC bleeds out over Bleed-Out Duration, then dies.
    ///
    /// Engine facts this relies on (ThunderRoad.dll 1.3.2, see README):
    /// - Health is drained through the public Creature.currentHealth setter, which
    ///   raises the health-change event. Creature.Damage is not used on purpose: it
    ///   applies damage multipliers (which would distort the duration) and raises
    ///   onCreatureHit for every tick.
    /// - The end is Creature.Kill(), the game's own kill (Energy, 99999).
    /// - The blood is the game's own "PenetrationDeepBleeding" effect (the drip a
    ///   deep stab leaves when the blade is pulled out, Damager.cs). Its particle
    ///   module runs on the End step, so each pulse is Spawn, Play, End. The effect
    ///   is tagged sensitiveContent "Blood", so the game's blood filter applies.
    /// </summary>
    internal sealed class BleedController
    {
        private const string BloodEffectId = "PenetrationDeepBleeding";

        private sealed class Bleed
        {
            public RagdollPart Part;
            public Vector3 LocalPoint;
            public float Elapsed;
            public int LastPulseIndex = BleedOutRules.NoPulseYet;
        }

        private readonly Dictionary<Creature, Bleed> bleeding = new Dictionary<Creature, Bleed>();
        private readonly List<Creature> scratch = new List<Creature>();
        private EffectData bloodEffect;
        private bool bloodEffectMissing;

        /// <summary>Starts the bleeding at the cut: part is the hit foot or leg, point the contact point.</summary>
        public void Start(Creature creature, RagdollPart part, Vector3 contactPoint)
        {
            if (bleeding.ContainsKey(creature))
            {
                return;
            }
            bleeding[creature] = new Bleed
            {
                Part = part,
                LocalPoint = part != null ? part.transform.InverseTransformPoint(contactPoint) : Vector3.zero,
            };
            ModLog.Verbose("BLEED START | " + GameAdapters.Label(creature) + " | "
                + Format(ModOptions.BleedOutDuration) + " s to bleed out");
        }

        /// <param name="isCrippled">Only crippled NPCs bleed; a released one stops for good.</param>
        public void Update(float deltaTime, System.Func<Creature, bool> isCrippled)
        {
            if (bleeding.Count == 0)
            {
                return;
            }
            scratch.Clear();
            scratch.AddRange(bleeding.Keys);
            foreach (var creature in scratch)
            {
                var bleed = bleeding[creature];
                var exists = creature != null && Creature.allActive.Contains(creature) && isCrippled(creature);
                var step = BleedOutRules.Step(new BleedInput
                {
                    Exists = exists,
                    IsKilled = exists && creature.isKilled,
                    FeatureOn = ModOptions.TendonEnabled && ModOptions.BleedOutEnabled,
                    Health = exists ? creature.currentHealth : 0f,
                    Elapsed = bleed.Elapsed,
                    LastPulseIndex = bleed.LastPulseIndex,
                    DeltaTime = deltaTime,
                    Duration = ModOptions.BleedOutDuration,
                    PulseInterval = ModOptions.BloodPulseInterval,
                });
                switch (step.Action)
                {
                    case BleedAction.Continue:
                        bleed.Elapsed = step.Elapsed;
                        bleed.LastPulseIndex = step.PulseIndex;
                        creature.currentHealth = step.Health;
                        if (step.Pulse)
                        {
                            Pulse(creature, bleed, step);
                        }
                        break;
                    case BleedAction.Kill:
                        bleeding.Remove(creature);
                        ModLog.Verbose("BLED OUT | " + GameAdapters.Label(creature) + " | after " + Format(step.Elapsed) + " s");
                        creature.Kill();
                        break;
                    case BleedAction.Forget:
                        bleeding.Remove(creature);
                        break;
                    case BleedAction.Pause:
                        break;
                }
            }
        }

        public void Clear()
        {
            bleeding.Clear();
        }

        private void Pulse(Creature creature, Bleed bleed, BleedStep step)
        {
            ModLog.Verbose("BLEED | " + GameAdapters.Label(creature)
                + " | health=" + Format(step.Health) + "/" + Format(creature.maxHealth)
                + " | t=" + Format(step.Elapsed) + "/" + Format(ModOptions.BleedOutDuration) + " s"
                + " | intensity=" + Format(step.PulseIntensity));
            if (!ModOptions.BloodEffects || bleed.Part == null)
            {
                return;
            }
            var data = BloodEffect();
            if (data == null)
            {
                return;
            }
            var transform = bleed.Part.transform;
            var point = transform.TransformPoint(bleed.LocalPoint);
            var outward = point - transform.position;
            var rotation = outward.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(outward) : Quaternion.identity;
            var instance = data.Spawn(point, rotation, transform, null, true, null, false, step.PulseIntensity, 1f);
            instance.SetIntensity(step.PulseIntensity);
            instance.Play();
            instance.End();
        }

        private EffectData BloodEffect()
        {
            if (bloodEffect == null && !bloodEffectMissing)
            {
                bloodEffect = Catalog.GetData<EffectData>(BloodEffectId, logWarning: false);
                if (bloodEffect == null)
                {
                    bloodEffectMissing = true;
                    ModLog.Warn("Blood effect '" + BloodEffectId + "' not in the catalog; bleeding without blood");
                }
            }
            return bloodEffect;
        }

        private static string Format(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
