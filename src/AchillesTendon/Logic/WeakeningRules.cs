namespace AchillesTendon.Logic
{
    /// <summary>One bleeding creature this frame, as plain values taken from the game and the menu.</summary>
    public struct WeakeningInput
    {
        /// <summary>Weakening is enabled in the menu.</summary>
        public bool FeatureOn;

        /// <summary>The creature is tracked by the bleed-out (crippled and not yet dead).</summary>
        public bool IsBleeding;

        /// <summary>Bleed-out progress, 0 = fresh cut, 1 = dead.</summary>
        public float Progress;

        /// <summary>Crawl speed factor reached at the end (1 = no slowdown).</summary>
        public float WeakestCrawlSpeed;

        /// <summary>Scream interval multiplier reached at the end (1 = no slowdown).</summary>
        public float ScreamSlowdown;

        /// <summary>Progress from which screams turn into heavy breathing.</summary>
        public float GaspingFrom;
    }

    public struct Weakening
    {
        /// <summary>Multiplies Crawl Speed.</summary>
        public float CrawlSpeedFactor;

        /// <summary>Multiplies Scream Interval.</summary>
        public float ScreamIntervalFactor;

        /// <summary>Heavy breathing instead of a pain scream.</summary>
        public bool Gasping;
    }

    /// <summary>Build step 5, "weaker/slower over time": everything scales linearly with the bleed-out.</summary>
    public static class WeakeningRules
    {
        public static readonly Weakening FullStrength = new Weakening { CrawlSpeedFactor = 1f, ScreamIntervalFactor = 1f };

        public static Weakening Compute(WeakeningInput input)
        {
            if (!input.FeatureOn || !input.IsBleeding)
            {
                return FullStrength;
            }
            var p = input.Progress < 0f ? 0f : input.Progress > 1f ? 1f : input.Progress;
            return new Weakening
            {
                CrawlSpeedFactor = Lerp(1f, input.WeakestCrawlSpeed, p),
                ScreamIntervalFactor = Lerp(1f, input.ScreamSlowdown, p),
                Gasping = p >= input.GaspingFrom,
            };
        }

        private static float Lerp(float from, float to, float t) => from + (to - from) * t;
    }
}
