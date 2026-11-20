namespace AchillesTendon.Logic
{
    /// <summary>How a crippled NPC is kept down; rolled per NPC from Lie Flat Chance.</summary>
    public enum DownedPose
    {
        /// <summary>The game's own injured/crawl animations: lies injured, crawls when it moves.</summary>
        Crawl = 0,

        /// <summary>Full ragdoll on the ground (Destabilized), held there.</summary>
        LieFlat = 1,
    }

    /// <summary>What a heel cut does; rolled once from Instant Death Chance, before the pose.</summary>
    public enum CutOutcome
    {
        /// <summary>Crippled: down for good, then the pose rolls (lie flat, sit and crawl, kneel).</summary>
        Cripple = 0,

        /// <summary>The cut kills at once: the game's own death (dying animation, kill events) with the cutting hit.</summary>
        InstantDeath = 1,
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
        /// <summary>Instant Death Chance (Nadi, 2026-11-20): whether a heel cut kills at once or cripples.</summary>
        public static CutOutcome OutcomeFor(float instantDeathChance, float roll)
        {
            return Chance.Hits(instantDeathChance, roll) ? CutOutcome.InstantDeath : CutOutcome.Cripple;
        }

        /// <summary>Lie Flat Chance (Nadi, 2026-11-17): the pose of a freshly crippled NPC.</summary>
        public static DownedPose PoseFor(float lieFlatChance, float roll)
        {
            return Chance.Hits(lieFlatChance, roll) ? DownedPose.LieFlat : DownedPose.Crawl;
        }

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

        /// <summary>
        /// Crawl Turn Speed: the locomotion turn speed of a crawling NPC, from the game's own value.
        /// The game's AI turns an NPC by a share of the remaining angle each frame, in proportion to
        /// locomotion.turnSpeed (BrainModuleMove.UpdateTurn, 1.3.2), so the factor slows the whole
        /// turn. Never faster than the game; below the menu's minimum counts as the minimum, since an
        /// NPC that can hardly turn would never face the player and never swing.
        /// </summary>
        public static float CrawlTurnSpeed(float gameTurnSpeed, float factor)
        {
            var f = System.Math.Min(1f, System.Math.Max(OptionSpecs.CrawlTurnSpeed.Min, factor));
            return gameTurnSpeed * f;
        }
    }
}
