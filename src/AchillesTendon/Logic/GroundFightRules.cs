namespace AchillesTendon.Logic
{
    /// <summary>One crippled creature this frame, as plain values taken from the game and the menu.</summary>
    public struct GroundFightInput
    {
        /// <summary>Achilles Tendon and Ground Fighting are both enabled.</summary>
        public bool FeatureOn;

        /// <summary>Lying on the ground and not getting up.</summary>
        public bool IsDown;

        /// <summary>Crawl pose: a standing ragdoll whose own AI attacks; no lying-only arm work.</summary>
        public bool IsCrawling;

        public bool AttacksOn;

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
        /// <summary>Arms are pinned harder to the animation; false = the game's own lying value.</summary>
        public bool StrongArms;

        public bool Attack;

        /// <summary>Crawl pose: play the swing on the upper-body layer so the legs stay down.</summary>
        public bool UpperBodyOnly;

        public bool Scream;
    }

    public static class GroundFightRules
    {
        public static GroundFightDecision Decide(GroundFightInput input)
        {
            if (!input.FeatureOn || !input.IsDown)
            {
                return new GroundFightDecision();
            }
            return new GroundFightDecision
            {
                StrongArms = !input.IsCrawling,
                UpperBodyOnly = input.IsCrawling,
                Attack = input.AttacksOn
                    && input.HasAttackClip
                    && !input.IsPlayingAnimation
                    && input.DistanceToPlayer <= input.AttackRange
                    && input.SinceLastAttack >= input.AttackInterval,
                Scream = input.ScreamsOn && input.SinceLastScream >= input.ScreamInterval,
            };
        }
    }
}
