namespace AchillesTendon.Logic
{
    /// <summary>One crippled creature this frame, as plain values taken from the game and the menu.</summary>
    public struct GroundFightInput
    {
        /// <summary>Achilles Tendon and Ground Fighting are both enabled.</summary>
        public bool FeatureOn;

        /// <summary>Lying on the ground and not getting up.</summary>
        public bool IsDown;

        /// <summary>Crawl pose. Only a crawling NPC swings: lying flat, the game's animation does not move the body.</summary>
        public bool IsCrawling;

        public bool AttacksOn;

        /// <summary>Rolled once when the tendon was cut (Ground Attack Chance): this NPC fights from the ground.</summary>
        public bool IsAttacker;

        /// <summary>The creature has attack animations (it held a weapon in a melee stance).</summary>
        public bool HasAttackClip;

        /// <summary>A dynamic animation (the last swing) is still playing.</summary>
        public bool IsPlayingAnimation;

        public float DistanceToPlayer;
        public float AttackRange;
        public float SinceLastAttack;
        public float AttackInterval;

        public bool ScreamsOn;
        public float SinceLastScream;
        public float ScreamInterval;
    }

    public struct GroundFightDecision
    {
        /// <summary>Swing on the upper-body layer (a full-body clip would raise the crawler upright).</summary>
        public bool Attack;

        public bool Scream;

        /// <summary>A swing is due but the NPC has no attack clips (cut before it stood in a melee
        /// stance): look for them again, then swing if found.</summary>
        public bool LookForClips;
    }

    public static class GroundFightRules
    {
        /// <summary>Ground Attack Chance: whether a freshly crippled NPC fights from the ground.</summary>
        public static bool IsAttacker(float chance, float roll)
        {
            return Chance.Hits(chance, roll);
        }

        public static GroundFightDecision Decide(GroundFightInput input)
        {
            if (!input.FeatureOn || !input.IsDown)
            {
                return new GroundFightDecision();
            }
            var swingDue = input.IsCrawling
                && input.AttacksOn
                && input.IsAttacker
                && !input.IsPlayingAnimation
                && input.DistanceToPlayer <= input.AttackRange
                && input.SinceLastAttack >= input.AttackInterval;
            return new GroundFightDecision
            {
                Attack = swingDue && input.HasAttackClip,
                LookForClips = swingDue && !input.HasAttackClip,
                Scream = input.ScreamsOn && input.SinceLastScream >= input.ScreamInterval,
            };
        }
    }
}
