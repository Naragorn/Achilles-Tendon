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
    public static class CrawlGaitRules
    {
        public static bool IsMoving(bool wasMoving, float speed, float strafe, float start, float stop)
        {
            var magnitude = Math.Max(Math.Abs(speed), Math.Abs(strafe));
            var stopAt = Math.Min(stop, start);
            return wasMoving ? magnitude >= stopAt : magnitude >= start;
        }
    }
}
