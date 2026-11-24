using System;
using System.Collections.Generic;
using System.Reflection;

namespace AchillesTendon.Logic
{
    /// <summary>A kill (EventManager.onCreatureKill), as plain values taken from the game.</summary>
    public struct KillInput
    {
        /// <summary>DMCS Adrenaline compat switched on in the menu.</summary>
        public bool CompatOn;

        public bool TargetIsPlayer;

        /// <summary>The dying creature had a cut tendon (it crawled or lay crippled).</summary>
        public bool WasCrippled;

        /// <summary>The killing hit is the very hit that cut the tendon: it never crawled.</summary>
        public bool KillIsCrippleHit;

        /// <summary>The killing hit came from the player (weapon, thrown item, spell, fist, kick); false for the bleed-out's Creature.Kill().</summary>
        public bool DoneByPlayer;

        /// <summary>Menu value: adrenaline for the execution.</summary>
        public float Amount;
    }

    /// <summary>
    /// DMCS compat: which Achilles events raise DMCS's adrenaline and by how much.
    /// DMCS owns the meter (its max, window and ready gate); these rules only pick the amount.
    /// </summary>
    public static class DmcsCompatRules
    {
        /// <summary>Adrenaline for a heel cut that just crippled an NPC.</summary>
        public static float HeelCutGain(bool compatOn, float amount)
        {
            return compatOn && amount > 0f ? amount : 0f;
        }

        /// <summary>
        /// Whether the killing hit is the hit that cut the tendon. The game reuses a weapon's
        /// CollisionInstance objects (CollisionHandler.collisions, decompiled 1.3.2), so the same
        /// object alone is not enough: the cut that kills at once does so within its own frame.
        /// </summary>
        public static bool IsCrippleHit(bool sameObject, int crippleFrame, int killFrame)
        {
            return sameObject && crippleFrame == killFrame;
        }

        /// <summary>Adrenaline for the player killing an NPC that was already crippled.</summary>
        public static float KillGain(KillInput input)
        {
            if (!input.CompatOn || input.Amount <= 0f || input.TargetIsPlayer)
            {
                return 0f;
            }
            if (!input.WasCrippled || input.KillIsCrippleHit || !input.DoneByPlayer)
            {
                return 0f;
            }
            return input.Amount;
        }
    }

    public enum DmcsGainOutcome
    {
        /// <summary>DMCS is not loaded, or it has no AddExternal of the expected shape.</summary>
        NotInstalled,

        /// <summary>DMCS added the amount to its meter.</summary>
        Applied,

        /// <summary>DMCS refused it (Adrenaline off, or the meter is full and waiting for the critical).</summary>
        Refused,

        /// <summary>The call threw; the exception is not passed on.</summary>
        Failed,
    }

    /// <summary>
    /// Finds DMCS.Adrenaline.AddExternal(float, string) among the loaded assemblies. The game
    /// loads every mod DLL with Assembly.Load into the same AppDomain (ModManager.TryLoadAssembly),
    /// so Achilles needs no reference to DMCS and works without it.
    /// </summary>
    public static class DmcsBridge
    {
        public const string AssemblyName = "DMCS";
        public const string TypeName = "DMCS.Adrenaline";
        public const string MethodName = "AddExternal";

        public static MethodInfo Resolve(IEnumerable<Assembly> assemblies, string assemblyName, string typeName, string methodName)
        {
            foreach (var assembly in assemblies)
            {
                if (assembly == null || !string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
                {
                    continue;
                }
                var type = assembly.GetType(typeName, false);
                var method = type?.GetMethod(
                    methodName,
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(float), typeof(string) },
                    null);
                if (method != null && method.ReturnType == typeof(bool))
                {
                    return method;
                }
            }
            return null;
        }

        public static DmcsGainOutcome Invoke(MethodInfo target, float amount, string source)
        {
            return Invoke(target, amount, source, out _);
        }

        /// <summary>Calls DMCS; a failure is returned as Failed with its exception, never thrown.</summary>
        public static DmcsGainOutcome Invoke(MethodInfo target, float amount, string source, out Exception error)
        {
            error = null;
            if (target == null)
            {
                return DmcsGainOutcome.NotInstalled;
            }
            try
            {
                return (bool)target.Invoke(null, new object[] { amount, source })
                    ? DmcsGainOutcome.Applied
                    : DmcsGainOutcome.Refused;
            }
            catch (Exception ex)
            {
                error = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                return DmcsGainOutcome.Failed;
            }
        }
    }
}
