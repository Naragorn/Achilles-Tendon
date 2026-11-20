using System;
using System.Collections.Generic;
using System.Globalization;
using AchillesTendon.Logic;
using ThunderRoad;

namespace AchillesTendon
{
    /// <summary>
    /// DMCS compat (Nadi, 2026-11-17): a heel cut (also one that kills at once) and the player's kill of a crippled NPC
    /// fill DMCS's Adrenaline meter through DMCS.Adrenaline.AddExternal(float, string).
    /// DMCS is looked up by name on every gain (rare events), so the mods' load order does
    /// not matter and Achilles runs unchanged without DMCS.
    /// </summary>
    public sealed class DmcsAdrenaline
    {
        public const string HeelCutSource = "AchillesHeelCut";
        public const string CrippledKillSource = "AchillesCrippledKill";

        /// <summary>The hit that cut each crippled NPC's tendon: killing with that same hit is no execution.</summary>
        private readonly Dictionary<Creature, CollisionInstance> crippleHits = new Dictionary<Creature, CollisionInstance>();

        private bool loggedMissing;

        public void OnCrippled(Creature creature, CollisionInstance hit)
        {
            crippleHits[creature] = hit;
            Give(DmcsCompatRules.HeelCutGain(ModOptions.DmcsAdrenaline, ModOptions.HeelCutAdrenaline), HeelCutSource, creature);
        }

        /// <summary>
        /// Instant Death: the heel cut's own amount. The kill itself is the cutting hit on an NPC that was never
        /// crippled, so OnKill gives nothing for it, as for a heel cut whose damage kills.
        /// </summary>
        public void OnKilledByCut(Creature creature)
        {
            Give(DmcsCompatRules.HeelCutGain(ModOptions.DmcsAdrenaline, ModOptions.HeelCutAdrenaline), HeelCutSource, creature);
        }

        public void OnKill(Creature creature, CollisionInstance hit, bool wasCrippled)
        {
            crippleHits.TryGetValue(creature, out var crippleHit);
            crippleHits.Remove(creature);
            var gain = DmcsCompatRules.KillGain(new KillInput
            {
                CompatOn = ModOptions.DmcsAdrenaline,
                TargetIsPlayer = creature.isPlayer,
                WasCrippled = wasCrippled,
                KillIsCrippleHit = hit != null && ReferenceEquals(hit, crippleHit),
                // Not CollisionInstance.IsDoneByPlayer(): for a punch or kick it asks the player's own
                // lastInteractionCreature (decompiled 1.3.2), so a fist kill would not count.
                DoneByPlayer = KilledByPlayer(hit),
                Amount = ModOptions.CrippledKillAdrenaline,
            });
            Give(gain, CrippledKillSource, creature);
        }

        private static bool KilledByPlayer(CollisionInstance hit)
        {
            if (hit == null)
            {
                return false;
            }
            var attacker = GameAdapters.Attacker(hit);
            return attacker != null && attacker.isPlayer;
        }

        public void Clear()
        {
            crippleHits.Clear();
        }

        private void Give(float amount, string source, Creature creature)
        {
            if (amount <= 0f)
            {
                return;
            }
            var target = DmcsBridge.Resolve(
                AppDomain.CurrentDomain.GetAssemblies(), DmcsBridge.AssemblyName, DmcsBridge.TypeName, DmcsBridge.MethodName);
            var outcome = DmcsBridge.Invoke(target, amount, source, out var error);
            switch (outcome)
            {
                case DmcsGainOutcome.NotInstalled:
                    if (!loggedMissing)
                    {
                        loggedMissing = true;
                        ModLog.Verbose("DMCS | not found (no " + DmcsBridge.TypeName + "." + DmcsBridge.MethodName + "), adrenaline skipped");
                    }
                    break;
                case DmcsGainOutcome.Failed:
                    ModLog.Error("DMCS | " + source + " failed: " + error);
                    break;
                default:
                    ModLog.Verbose("DMCS | " + source + " +" + amount.ToString("0", CultureInfo.InvariantCulture)
                        + " | " + GameAdapters.Label(creature) + " | " + outcome);
                    break;
            }
        }
    }
}
