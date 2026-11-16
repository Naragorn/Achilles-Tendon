namespace AchillesTendon.Logic
{
    /// <summary>What to do with a crippled creature this frame.</summary>
    public enum CrippleAction
    {
        /// <summary>It lies on the ground: keep the no-stand-up hold in place.</summary>
        HoldDown,

        /// <summary>It is up (or getting up) although crippled: knock it down again and hold it.</summary>
        Collapse,

        /// <summary>Remove the hold and stop tracking it; the game lets it stand up by itself.</summary>
        Release,

        /// <summary>Stop tracking it without touching it (dead or despawned; the game already cleared the hold).</summary>
        Forget,
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
        public static CrippleAction Decide(CrippledCreatureState state, bool tendonEnabled, bool standUpWhenDisabled)
        {
            if (!state.Exists || state.IsKilled)
            {
                return CrippleAction.Forget;
            }
            if (!tendonEnabled)
            {
                return standUpWhenDisabled ? CrippleAction.Release : CrippleAction.HoldDown;
            }
            return state.IsDown ? CrippleAction.HoldDown : CrippleAction.Collapse;
        }
    }
}
