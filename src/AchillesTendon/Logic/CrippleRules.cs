namespace AchillesTendon.Logic
{
    /// <summary>
    /// How a crippled NPC is kept down. Shown as a dropdown in the Mod Menu; keep the
    /// default first (the game's mod menu always falls back to the first enum entry).
    /// </summary>
    public enum DownedPose
    {
        /// <summary>The game's own injured/crawl animations: lies injured, crawls when it moves.</summary>
        Crawl = 0,

        /// <summary>Full ragdoll on the ground (Destabilized), held there.</summary>
        LieFlat = 1,
    }

    /// <summary>What to do with a crippled creature this frame.</summary>
    public enum CrippleAction
    {
        /// <summary>It lies on the ground: keep the no-stand-up hold in place.</summary>
        HoldDown,

        /// <summary>Crawl pose: keep the injured/crawl animation blend and the slow movement.</summary>
        HoldCrawl,

        /// <summary>It is up (or getting up) although crippled: knock it down again and hold it.</summary>
        Collapse,

        /// <summary>Remove the hold and stop tracking it; the game lets it stand up by itself.</summary>
        Release,

        /// <summary>Stop tracking it without touching it (dead or despawned; the game already cleared the hold).</summary>
        Forget,
    }

    /// <summary>Rotation drive of one animation pin (Ragdoll.Bone.SetPinRotationForce).</summary>
    public struct PinDrive
    {
        public float Spring;
        public float Damper;
        public float MaxForce;
    }

    /// <summary>A crippled creature, as plain values taken from the game.</summary>
    public struct CrippledCreatureState
    {
        /// <summary>False once the creature was despawned or is no longer active.</summary>
        public bool Exists;

        public bool IsKilled;

        /// <summary>Lying on the ground (destabilized) and not in the middle of getting up.</summary>
        public bool IsDown;
    }

    public static class CrippleRules
    {
        /// <summary>
        /// Whether a heel hit cripples its target. Only NPCs for now: the player
        /// collapse belongs to build step 6, so a player heel hit is only reported.
        /// </summary>
        public static bool ShouldCripple(HeelHitResult result, bool targetIsPlayer, bool targetIsKilled, bool alreadyCrippled)
        {
            return result.IsHeelHit && !targetIsPlayer && !targetIsKilled && !alreadyCrippled;
        }

        /// <summary>Per-frame decision for one crippled creature.</summary>
        public static CrippleAction Decide(CrippledCreatureState state, bool tendonEnabled, bool standUpWhenDisabled, DownedPose pose)
        {
            if (!state.Exists || state.IsKilled)
            {
                return CrippleAction.Forget;
            }
            var hold = pose == DownedPose.Crawl ? CrippleAction.HoldCrawl : CrippleAction.HoldDown;
            if (!tendonEnabled)
            {
                return standUpWhenDisabled ? CrippleAction.Release : hold;
            }
            if (pose == DownedPose.Crawl)
            {
                // Crawling needs a standing ragdoll: a knocked-down NPC is left to get up
                // by itself, then the crawl blend holds it low again.
                return CrippleAction.HoldCrawl;
            }
            return state.IsDown ? CrippleAction.HoldDown : CrippleAction.Collapse;
        }

        /// <summary>
        /// Leg Strength: the legs' rotation pins while crawling, from the ragdoll's own
        /// standing values. Spring and max force scale with the multiplier, the damper with
        /// its square root (critical damping grows with sqrt(spring), so the damping ratio
        /// stays the game's). Below x1 counts as x1.
        /// </summary>
        public static PinDrive LegPin(float multiplier, float spring, float damper, float maxForce)
        {
            var m = System.Math.Max(1f, multiplier);
            return new PinDrive
            {
                Spring = spring * m,
                Damper = damper * (float)System.Math.Sqrt(m),
                MaxForce = maxForce * m,
            };
        }
    }
}
