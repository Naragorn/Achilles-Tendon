using System;

namespace AchillesTendon.Logic
{
    /// <summary>
    /// Steady Crawl Pose. The game's Human animator switches the crawl pose between its
    /// idle state (sitting, LayingInjured) and its moving state (Crawling) at |Speed| or
    /// |Strafe| = 0.1, with no hysteresis (Dual Wield / 1handed Idle &lt;-&gt; Moving, read
    /// from the controller). The AI's small corrective moves sit right at that line, so the
    /// pose flips every 0.25-0.5 s and the physical hips hang between the two heights
    /// ("sitting in the air", measured with the CRAWL POSE log). This adds the missing
    /// hysteresis: it only counts as moving from Start up, and as still again below Stop.
    /// </summary>
    /// <summary>Animator Speed/Strafe to write for a belly crawler this frame.</summary>
    public struct CrawlGait
    {
        public float Speed;
        public float Strafe;
    }

    public static class CrawlGaitRules
    {
        /// <summary>
        /// Just above the animator's idle/moving switch at |Speed| 0.1 (Human controller): a
        /// still belly crawler stays in the moving blend tree, between Crouch_Idle (Speed 0)
        /// and the face-down Crawling clip (Speed 0.16), instead of falling back to the
        /// idle states, which lie on the back. An engine threshold, not a tuning value.
        /// </summary>
        public const float BellyHoldSpeed = 0.12f;

        /// <summary>
        /// Belly crawl: always the forward Crawling clip (face down). CrawlBackwards lies on
        /// the back, so a belly crawler moving backwards or sideways plays the forward crawl
        /// at its own speed.
        /// </summary>
        public static CrawlGait BellyGait(bool moving, float speed, float strafe)
        {
            var magnitude = moving ? Math.Max(Math.Abs(speed), Math.Abs(strafe)) : 0f;
            return new CrawlGait { Speed = Math.Max(BellyHoldSpeed, magnitude), Strafe = 0f };
        }

        public static bool IsMoving(bool wasMoving, float speed, float strafe, float start, float stop)
        {
            var magnitude = Math.Max(Math.Abs(speed), Math.Abs(strafe));
            var stopAt = Math.Min(stop, start);
            return wasMoving ? magnitude >= stopAt : magnitude >= start;
        }
    }
}
