namespace AchillesTendon.Logic
{
    /// <summary>What happens to a crippled NPC's death.</summary>
    public static class DeathRules
    {
        /// <summary>
        /// A crawling NPC is a standing ragdoll kept low, so the game's BrainModuleDeath takes it
        /// for a standing NPC and plays a standing death clip, pinned for about 3 s: the body
        /// hangs stiffly above the ground. That dying animation is stopped; an NPC that was not
        /// crawling keeps the game's own death.
        /// </summary>
        public static bool StopDyingAnimation(bool wasCrawling, bool isDying)
        {
            return wasCrawling && isDying;
        }
    }
}
