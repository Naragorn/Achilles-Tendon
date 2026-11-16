using System;

namespace AchillesTendon.Logic
{
    /// <summary>What to do with a bleeding creature this frame.</summary>
    public enum BleedAction
    {
        /// <summary>Keep bleeding: set the new health, maybe spawn a blood pulse.</summary>
        Continue,

        /// <summary>Bleeding is switched off: nothing changes, progress is kept.</summary>
        Pause,

        /// <summary>Bled out: kill it.</summary>
        Kill,

        /// <summary>Dead or despawned: stop tracking it.</summary>
        Forget,
    }

    /// <summary>One bleeding creature, as plain values taken from the game and the menu.</summary>
    public struct BleedInput
    {
        public bool Exists;
        public bool IsKilled;

        /// <summary>Achilles Tendon and Bleed-Out are both enabled.</summary>
        public bool FeatureOn;

        public float Health;

        /// <summary>Seconds bled so far (paused time does not count).</summary>
        public float Elapsed;

        /// <summary>Index of the last blood pulse, or <see cref="BleedOutRules.NoPulseYet"/>.</summary>
        public int LastPulseIndex;

        public float DeltaTime;

        /// <summary>Bleed-Out Duration from the menu, read every frame so changes apply live.</summary>
        public float Duration;

        /// <summary>Seconds between blood pulses (and log lines).</summary>
        public float PulseInterval;
    }

    public struct BleedStep
    {
        public BleedAction Action;
        public float Health;
        public float Elapsed;

        /// <summary>A new pulse interval started this frame: spawn blood and log.</summary>
        public bool Pulse;

        public int PulseIndex;

        /// <summary>0..1, grows with every pulse and reaches 1 at the end of the duration.</summary>
        public float PulseIntensity;
    }

    public static class BleedOutRules
    {
        public const int NoPulseYet = -1;

        /// <summary>How far the bleed-out has run, 0 (fresh cut) to 1 (dead).</summary>
        public static float Progress(float elapsed, float duration)
        {
            if (duration <= 0f)
            {
                return 1f;
            }
            var progress = elapsed / duration;
            return progress < 0f ? 0f : progress > 1f ? 1f : progress;
        }

        /// <summary>
        /// Health drains linearly from whatever it is now to zero at the end of the
        /// duration. Extra damage (the player finishing it off) and healing simply
        /// change the starting point; the creature still dies at the end.
        /// </summary>
        public static BleedStep Step(BleedInput input)
        {
            var step = new BleedStep
            {
                Health = input.Health,
                Elapsed = input.Elapsed,
                PulseIndex = input.LastPulseIndex,
            };
            if (!input.Exists || input.IsKilled)
            {
                step.Action = BleedAction.Forget;
                return step;
            }
            if (!input.FeatureOn)
            {
                step.Action = BleedAction.Pause;
                return step;
            }

            var remainingBefore = input.Duration - input.Elapsed;
            var elapsedAfter = input.Elapsed + input.DeltaTime;
            step.Elapsed = elapsedAfter;
            if (input.Health <= 0f || remainingBefore <= 0f || elapsedAfter >= input.Duration)
            {
                step.Action = BleedAction.Kill;
                step.Health = 0f;
                return step;
            }

            step.Action = BleedAction.Continue;
            step.Health = input.Health * (input.Duration - elapsedAfter) / remainingBefore;
            step.PulseIndex = (int)Math.Floor(elapsedAfter / input.PulseInterval);
            step.Pulse = step.PulseIndex > input.LastPulseIndex;
            step.PulseIntensity = Math.Min(1f, (step.PulseIndex + 1) * input.PulseInterval / input.Duration);
            return step;
        }
    }
}
