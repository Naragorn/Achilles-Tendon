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
    /// <summary>Animator Speed/Strafe to write for a kneeling NPC this frame.</summary>
    public struct CrawlGait
    {
        public float Speed;
        public float Strafe;
    }

    public static class CrawlGaitRules
    {
        /// <summary>
        /// Just above the animator's idle/moving switch at |Speed| 0.1 (Human controller): a
        /// still kneeling NPC stays in the moving blend tree (between Crouch_Idle at Speed 0
        /// and Crawling at 0.16) instead of falling back to the idle states, which sit or lie
        /// on the back. An engine threshold, not a tuning value.
        /// </summary>
        public const float KneelHoldSpeed = 0.12f;

        /// <summary>
        /// Kneel pose (Kneel Chance): always the forward moving blend tree, never the idle
        /// states or CrawlBackwards (both on the back); moving backwards or sideways plays the
        /// forward motion at its own speed. Headless it kneels upright (probes 20261117-192804,
        /// -195433), the pose Nadi kept (2026-11-17).
        /// </summary>
        public static CrawlGait KneelGait(bool moving, float speed, float strafe)
        {
            var magnitude = moving ? Math.Max(Math.Abs(speed), Math.Abs(strafe)) : 0f;
            return new CrawlGait { Speed = Math.Max(KneelHoldSpeed, magnitude), Strafe = 0f };
        }

        public static bool IsMoving(bool wasMoving, float speed, float strafe, float start, float stop)
        {
            var magnitude = Math.Max(Math.Abs(speed), Math.Abs(strafe));
            var stopAt = Math.Min(stop, start);
            return wasMoving ? magnitude >= stopAt : magnitude >= start;
        }
    }
}
