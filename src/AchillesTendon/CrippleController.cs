using System.Collections.Generic;
using AchillesTendon.Logic;
using ThunderRoad;

namespace AchillesTendon
{
    /// <summary>
    /// Build step 3: a cut tendon takes the NPC down for good, either into the game's
    /// own injured/crawl animations (Downed Pose = Crawl) or flat on the ground (Lie Flat).
    ///
    /// Engine facts this relies on (ThunderRoad.dll 1.3.2, see README):
    /// - Knock-down: the same calls as Creature.PushFallback, CancelGetUp(false)
    ///   then Ragdoll.SetState(Destabilized).
    /// - Stand-up: only BrainModuleHitReaction.OnCycle calls Ragdoll.StandUp, and
    ///   only while Brain.noStandupModifiers is empty. Holding a modifier keeps the
    ///   NPC down; removing it lets the game stand it up by itself.
    /// - Brain.Stop clears all modifiers (death, despawn, and catching fire), so the
    ///   hold is re-applied every frame instead of once.
    /// </summary>
    internal sealed class CrippleController
    {
        private readonly HashSet<Creature> crippled = new HashSet<Creature>();
        private readonly HashSet<Creature> crawling = new HashSet<Creature>();

        // Crawling NPCs whose own melee AI we switched off, with the value to restore.
        private readonly Dictionary<Creature, bool> meleeBefore = new Dictionary<Creature, bool>();

        // Steady Crawl Pose: crawling NPCs that currently count as moving (CrawlGaitRules).
        private readonly HashSet<Creature> gaitMoving = new HashSet<Creature>();
        private readonly List<Creature> scratch = new List<Creature>();

        // Debug Logging only: when each crawling NPC last wrote a CRAWL POSE line.
        private readonly Dictionary<Creature, float> lastPoseLog = new Dictionary<Creature, float>();
        private const float PoseLogInterval = 0.5f;

        // Debug Logging only: crippled NPCs that died, watched for DeathWatchSeconds (DEATH POSE).
        private readonly Dictionary<Creature, float> deathWatch = new Dictionary<Creature, float>();
        private readonly Dictionary<Creature, float> deathLastLog = new Dictionary<Creature, float>();
        private readonly List<Creature> deathScratch = new List<Creature>();
        private const float DeathWatchSeconds = 6f;

        // Lie Flat Chance, rolled per NPC when its tendon is cut.
        private readonly Dictionary<Creature, DownedPose> poses = new Dictionary<Creature, DownedPose>();

        // Belly Crawl Chance, rolled per crawling NPC when its tendon is cut.
        private readonly HashSet<Creature> belly = new HashSet<Creature>();

        // The game's stab deaths (bas.jsondb, 1.3.2): with moveDirection Forward they are
        // DeathKneelForward 1-3, which end face down (clip root rotation, read from
        // animations_assets_all.bundle).
        private const string FallForwardAnimationId = "HumanStabDeaths";
        private const string FallForwardClipName = "KneelForward";

        public bool IsCrippled(Creature creature) => crippled.Contains(creature);

        public void Cripple(Creature creature)
        {
            crippled.Add(creature);
            var roll = UnityEngine.Random.value;
            var pose = CrippleRules.PoseFor(ModOptions.LieFlatChance, roll);
            poses[creature] = pose;
            var rolled = " (roll " + roll.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                + ", lie flat chance " + OptionSpecs.LieFlatChance.Label(ModOptions.LieFlatChance) + ")";
            if (pose == DownedPose.Crawl)
            {
                var bellyRoll = UnityEngine.Random.value;
                var onBelly = Chance.Hits(ModOptions.BellyCrawlChance, bellyRoll);
                if (onBelly)
                {
                    belly.Add(creature);
                }
                Crawl(creature, 1f);
                ModLog.Verbose("COLLAPSE | " + GameAdapters.Label(creature) + " | tendon cut, down to the injured/crawl pose" + rolled
                    + " | " + (onBelly ? "belly" : "back") + " (roll " + bellyRoll.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    + ", belly chance " + OptionSpecs.BellyCrawlChance.Label(ModOptions.BellyCrawlChance) + ")");
                if (onBelly)
                {
                    FallForward(creature);
                }
            }
            else
            {
                Collapse(creature);
                ModLog.Verbose("COLLAPSE | " + GameAdapters.Label(creature) + " | tendon cut, held on the ground" + rolled);
            }
        }

        private DownedPose PoseOf(Creature creature)
        {
            return poses.TryGetValue(creature, out var pose) ? pose : DownedPose.Crawl;
        }

        /// <summary>Crippled and in the crawl pose (a standing ragdoll kept low).</summary>
        public bool IsCrawling(Creature creature) => crawling.Contains(creature);

        /// <summary>Crippled and either crawling or lying on the ground.</summary>
        public bool IsDowned(Creature creature) => crawling.Contains(creature) || (crippled.Contains(creature) && StateOf(creature).IsDown);

        /// <param name="crawlSpeedFactor">Weakening from the bleed-out, multiplies Crawl Speed.</param>
        public void Update(System.Func<Creature, float> crawlSpeedFactor)
        {
            LogDeathPoses();
            if (crippled.Count == 0)
            {
                return;
            }
            scratch.Clear();
            scratch.AddRange(crippled);
            foreach (var creature in scratch)
            {
                var action = CrippleRules.Decide(StateOf(creature), ModOptions.TendonEnabled, ModOptions.StandUpWhenDisabled, PoseOf(creature));
                switch (action)
                {
                    case CrippleAction.HoldCrawl:
                        Crawl(creature, crawlSpeedFactor(creature));
                        break;
                    case CrippleAction.HoldDown:
                        StopCrawl(creature);
                        Hold(creature);
                        break;
                    case CrippleAction.Collapse:
                        StopCrawl(creature);
                        Collapse(creature);
                        ModLog.Verbose("RE-COLLAPSE | " + GameAdapters.Label(creature) + " | was getting up while crippled");
                        break;
                    case CrippleAction.Release:
                        StopCrawl(creature);
                        Release(creature);
                        crippled.Remove(creature);
                        poses.Remove(creature);
                        belly.Remove(creature);
                        ModLog.Verbose("RELEASE | " + GameAdapters.Label(creature) + " | feature off, may stand up");
                        break;
                    case CrippleAction.Forget:
                        if (creature != null)
                        {
                            if (ModOptions.DebugLogging && creature.isKilled)
                            {
                                deathWatch[creature] = UnityEngine.Time.time;
                                deathLastLog[creature] = float.NegativeInfinity;
                            }
                            StopCrawl(creature);
                        }
                        crawling.Remove(creature);
                        meleeBefore.Remove(creature);
                        gaitMoving.Remove(creature);
                        lastPoseLog.Remove(creature);
                        crippled.Remove(creature);
                        poses.Remove(creature);
                        belly.Remove(creature);
                        break;
                }
            }
        }

        /// <summary>Lets every crippled NPC stand up again (mod disabled or unloaded).</summary>
        public void ReleaseAll()
        {
            foreach (var creature in crippled)
            {
                if (creature != null && !creature.isKilled)
                {
                    StopCrawl(creature);
                    Release(creature);
                }
            }
            crippled.Clear();
            poses.Clear();
            belly.Clear();
            crawling.Clear();
            meleeBefore.Clear();
            gaitMoving.Clear();
            lastPoseLog.Clear();
        }

        private static CrippledCreatureState StateOf(Creature creature)
        {
            if (creature == null || !Creature.allActive.Contains(creature))
            {
                return new CrippledCreatureState { Exists = false };
            }
            return new CrippledCreatureState
            {
                Exists = true,
                IsKilled = creature.isKilled,
                IsDown = creature.state == Creature.State.Destabilized && !creature.ragdoll.standingUp,
            };
        }

        private void Collapse(Creature creature)
        {
            if (creature.ragdoll.standingUp)
            {
                creature.ragdoll.CancelGetUp(resetState: false);
            }
            creature.ragdoll.SetState(Ragdoll.State.Destabilized);
            Hold(creature);
        }

        // Crawl pose, the same animator settings Burning uses for a burning NPC that drops:
        // a low Height and Injured = 1 blend the base layer's idle states into
        // Laying/LayingInjured and its moving states into Crawling/CrawlBackwards
        // (Human animator controller, read from animations_assets_all.bundle). The body
        // stays a standing ragdoll, so the AI keeps moving (slowly) and attacking.
        // Re-applied every frame: the game resets Height on state changes and while blocking.
        private void Crawl(Creature creature, float speedFactor)
        {
            LogPose(creature);
            if (creature.brain != null)
            {
                creature.brain.RemoveNoStandUpModifier(this);
            }
            creature.SetAnimatorHeightRatio(ModOptions.CrawlBodyHeight);
            creature.animator.SetFloat(Brain.hashInjured, 1f);
            creature.locomotion.SetAllSpeedModifiers(this, ModOptions.CrawlSpeed * speedFactor);
            creature.ragdoll.forcePhysic.Add(this);
            crawling.Add(creature);
            SteadyGait(creature);
            SetLegStrength(creature, ModOptions.LegStrength);
            // The AI's own attack clips move the whole body and raise a crawling NPC
            // upright (headless pictures), so its melee is off while crawling; swings come
            // from GroundFightController on the upper-body layer (Ground Attacks).
            var melee = Melee(creature);
            if (melee != null && !meleeBefore.ContainsKey(creature))
            {
                meleeBefore[creature] = melee.meleeEnabled;
            }
            if (melee != null)
            {
                melee.meleeEnabled = false;
            }
        }

        // Steady Crawl Pose, see CrawlGaitRules. Runs in ScriptUpdate, which the game calls
        // after every Creature.ManagedUpdate in the same frame (UpdateManager.Update, then
        // ThunderScriptLoop), so it overrides that frame's Speed/Strafe from
        // Creature.UpdateLocomotionAnimation. Decided on the raw local velocity the game
        // derives them from, not on the animator values: those are damped and, once zeroed
        // here, would only creep back up from 0.
        private void SteadyGait(Creature creature)
        {
            if (belly.Contains(creature))
            {
                BellyGait(creature);
                return;
            }
            if (!ModOptions.SteadyCrawlPose)
            {
                gaitMoving.Remove(creature);
                return;
            }
            var raw = RawGait(creature);
            var was = gaitMoving.Contains(creature);
            var moving = CrawlGaitRules.IsMoving(was, raw.x, raw.y, ModOptions.CrawlStartSpeed, ModOptions.CrawlStopSpeed);
            if (moving != was)
            {
                ModLog.Verbose("GAIT | " + GameAdapters.Label(creature) + " | " + (moving ? "crawling" : "sitting")
                    + " | raw speed=" + raw.x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    + " strafe=" + raw.y.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            }
            if (moving)
            {
                gaitMoving.Add(creature);
                return;
            }
            gaitMoving.Remove(creature);
            creature.animator.SetFloat(Creature.hashSpeed, 0f);
            creature.animator.SetFloat(Creature.hashStrafe, 0f);
        }

        // Belly crawl: always the face-down forward Crawling clip, never the idle states or
        // CrawlBackwards, which lie on the back (CrawlGaitRules.BellyGait). Start/stop use the
        // same hysteresis as Steady Crawl Pose.
        private void BellyGait(Creature creature)
        {
            var raw = RawGait(creature);
            var was = gaitMoving.Contains(creature);
            var moving = CrawlGaitRules.IsMoving(was, raw.x, raw.y, ModOptions.CrawlStartSpeed, ModOptions.CrawlStopSpeed);
            if (moving)
            {
                gaitMoving.Add(creature);
            }
            else
            {
                gaitMoving.Remove(creature);
            }
            var gait = CrawlGaitRules.BellyGait(moving, raw.x, raw.y);
            creature.animator.SetFloat(Creature.hashSpeed, gait.Speed);
            creature.animator.SetFloat(Creature.hashStrafe, gait.Strafe);
        }

        // Falls forward with one of the game's own face-down stab deaths, played as a
        // one-shot on the FullBody layer (Creature.PlayAnimation); when it ends the animator
        // goes back to the base layer, where BellyGait holds the face-down crawl.
        private static void FallForward(Creature creature)
        {
            var data = Catalog.GetData<AnimationData>(FallForwardAnimationId, logWarning: false);
            AnimationData.Clip clip = null;
            if (data != null)
            {
                // Pick(Forward) returned Sword1h_Death_back in the first probe (run
                // 20261117-192804), so the face-down clips are chosen by name.
                var candidates = new List<AnimationData.Clip>();
                var listed = string.Empty;
                foreach (var c in data.animationClips)
                {
                    listed += (c.animationClip != null ? c.animationClip.name : "null") + "/" + c.moveDirection + " ";
                    if (c.animationClip != null && c.animationClip.name.IndexOf(FallForwardClipName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        candidates.Add(c);
                    }
                }
                ModLog.Verbose("FALL FORWARD clips | " + FallForwardAnimationId + " | " + listed);
                clip = candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
            }
            if (clip == null || clip.animationClip == null)
            {
                ModLog.Verbose("FALL FORWARD skipped | " + GameAdapters.Label(creature) + " | no forward clip in " + FallForwardAnimationId);
                return;
            }
            creature.PlayAnimation(clip.animationClip, loop: false);
            ModLog.Verbose("FALL FORWARD | " + GameAdapters.Label(creature) + " | clip=" + clip.animationClip.name);
        }

        private const RagdollPart.Type LegParts =
            RagdollPart.Type.LeftLeg | RagdollPart.Type.RightLeg | RagdollPart.Type.LeftFoot | RagdollPart.Type.RightFoot;

        // Leg Strength. After a crawl stops, only the lower legs touch the ground while the
        // hips hang 0.3 m above the sitting pose for 3-4 s (CRAWL BODY log): the body rests
        // on its shins and the leg pins fold them into the sitting pose only slowly. From the
        // ragdoll's own standing values (Bone.ResetPinForce uses the same three). Re-applied
        // every frame: the game resets pins on ragdoll state changes (Ragdoll.ResetPinForce).
        private static void SetLegStrength(Creature creature, float multiplier)
        {
            var ragdoll = creature.ragdoll;
            var pin = CrippleRules.LegPin(multiplier, ragdoll.springRotationForce, ragdoll.damperRotationForce, ragdoll.maxRotationForce);
            foreach (var part in ragdoll.parts)
            {
                if (part != ragdoll.rootPart && (part.type & LegParts) != 0 && !part.isSliced && part.bone != null && part.bone.animationJoint != null)
                {
                    part.bone.SetPinRotationForce(pin.Spring, pin.Damper, pin.MaxForce);
                }
            }
        }

        // Forward (x) and sideways (y) speed in the creature's own frame, as
        // Creature.UpdateLocomotionAnimation computes Speed/Strafe (without its in-air factor).
        private static UnityEngine.Vector2 RawGait(Creature creature)
        {
            var local = creature.transform.InverseTransformDirection(creature.currentLocomotion.velocity);
            var scale = creature.transform.lossyScale;
            return new UnityEngine.Vector2(local.z / scale.z, local.x / scale.x);
        }

        // Diagnosis of the "floating sit" (Debug Logging): what the animator's Height was
        // before this frame's write (another module may have changed it since the last
        // frame), how high the hips are above the creature's ground point, and which AI
        // states were active.
        private void LogPose(Creature creature)
        {
            if (!ModOptions.DebugLogging)
            {
                return;
            }
            var now = UnityEngine.Time.time;
            if (lastPoseLog.TryGetValue(creature, out var last) && now - last < PoseLogInterval)
            {
                return;
            }
            lastPoseLog[creature] = now;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var hips = creature.ragdoll.rootPart != null
                ? creature.ragdoll.rootPart.transform.position.y - creature.transform.position.y
                : float.NaN;
            var brain = creature.brain;
            ModLog.Verbose("CRAWL POSE | " + GameAdapters.Label(creature)
                + " | height before=" + creature.GetAnimatorHeightRatio().ToString("0.00", ci)
                + " set=" + ModOptions.CrawlBodyHeight.ToString("0.00", ci)
                + " | hips=" + hips.ToString("0.00", ci) + " m"
                + " animated hips=" + AnimatedHeight(creature, creature.ragdoll.rootPart).ToString("0.00", ci)
                + " feet=" + FootHeights(creature, ci)
                + " | " + RootPin(creature, ci)
                + " | under hips: " + UnderHips(creature, ci)
                + " | grounded=" + creature.locomotion.isGrounded
                + " | defending=" + (brain != null && brain.isDefending)
                + " dodging=" + (brain != null && brain.isDodging)
                + " dynamicAnim=" + creature.isPlayingDynamicAnimation
                + " block=" + creature.animator.GetFloat(Brain.hashBlock).ToString("0.00", ci)
                + " | anim state=" + creature.animator.GetCurrentAnimatorStateInfo(0).shortNameHash
                + " next=" + (creature.animator.IsInTransition(0) ? creature.animator.GetNextAnimatorStateInfo(0).shortNameHash.ToString(ci) : "-")
                + " transition=" + (creature.animator.IsInTransition(0) ? creature.animator.GetAnimatorTransitionInfo(0).normalizedTime.ToString("0.00", ci) : "-")
                + " speed=" + creature.animator.GetFloat(Creature.hashSpeed).ToString("0.00", ci)
                + " strafe=" + creature.animator.GetFloat(Creature.hashStrafe).ToString("0.00", ci)
                + " | move=" + creature.currentLocomotion.horizontalSpeed.ToString("0.00", ci) + " m/s"
                + " turn=" + creature.currentLocomotion.angularSpeed.ToString("0.0", ci) + " deg/s"
                + " raw speed=" + RawGait(creature).x.ToString("0.00", ci) + " strafe=" + RawGait(creature).y.ToString("0.00", ci)
                + " gait=" + (!ModOptions.SteadyCrawlPose ? "game" : gaitMoving.Contains(creature) ? "moving" : "still")
                + " | state=" + creature.state);
            ModLog.Verbose("CRAWL BODY | " + GameAdapters.Label(creature) + " | " + BodySupport(creature, ci));
        }

        // Diagnosis of a dead crippled NPC hanging stiffly above the ground (2026-11-17):
        // ragdoll state, animator, and per part the lowest collider point above the terrain
        // (cm), vertical velocity and the animation pin's rotation spring.
        private void LogDeathPoses()
        {
            if (deathWatch.Count == 0)
            {
                return;
            }
            var now = UnityEngine.Time.time;
            deathScratch.Clear();
            deathScratch.AddRange(deathWatch.Keys);
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            foreach (var creature in deathScratch)
            {
                var since = now - deathWatch[creature];
                if (creature == null || since > DeathWatchSeconds)
                {
                    deathWatch.Remove(creature);
                    deathLastLog.Remove(creature);
                    continue;
                }
                if (now - deathLastLog[creature] < PoseLogInterval)
                {
                    continue;
                }
                deathLastLog[creature] = now;
                var ragdoll = creature.ragdoll;
                var ground = TerrainBelow(creature);
                var text = "DEATH POSE | " + GameAdapters.Label(creature) + " | t=" + since.ToString("0.0", ci) + " s"
                    + " | ragdoll=" + ragdoll.state + " animator=" + creature.animator.enabled
                    + " heightRatio=" + creature.GetAnimatorHeightRatio().ToString("0.00", ci)
                    + " | terrain=" + ground.ToString("0.00", ci) + " creatureY=" + creature.transform.position.y.ToString("0.00", ci) + " | ";
                foreach (var part in ragdoll.parts)
                {
                    var joint = part.bone != null ? part.bone.animationJoint : null;
                    text += part.type + " low=" + (LowestPoint(part.colliderGroup != null ? part.colliderGroup.colliders : null, ground) * 100f).ToString("0", ci)
                        + " vy=" + (part.physicBody != null ? part.physicBody.velocity.y.ToString("0.00", ci) : "-")
                        + " pin=" + (joint != null && joint.gameObject.activeInHierarchy ? joint.slerpDrive.positionSpring.ToString("0", ci) : "off")
                        + (part.physicBody != null && part.physicBody.isKinematic ? " kin" : string.Empty) + "; ";
                }
                ModLog.Verbose(text);
            }
        }

        // Height of the first collider below the hips that does not belong to the creature.
        private static float TerrainBelow(Creature creature)
        {
            var root = creature.ragdoll.rootPart;
            if (root == null)
            {
                return creature.transform.position.y;
            }
            var hits = UnityEngine.Physics.RaycastAll(root.transform.position + UnityEngine.Vector3.up, UnityEngine.Vector3.down, 5f, ~0, UnityEngine.QueryTriggerInteraction.Ignore);
            var best = float.NaN;
            var bestDistance = float.PositiveInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Creature>() == creature || hit.collider.attachedRigidbody != null)
                {
                    continue;
                }
                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    best = hit.point.y;
                }
            }
            return float.IsNaN(best) ? creature.transform.position.y : best;
        }

        // Diagnosis of the slow sink after a crawl stops (Debug Logging): per body part the
        // lowest collider point above the ground point (cm) and its vertical velocity; the
        // held items' lowest point; the locomotion capsule. Shows what touches the ground
        // while the hips hang (2026-11-17: only the lower legs). Joint.currentForce of the
        // animation pins read 0 throughout, so it is not logged.
        private static string BodySupport(Creature creature, System.Globalization.CultureInfo ci)
        {
            var ground = creature.transform.position.y;
            var text = string.Empty;
            foreach (var part in creature.ragdoll.parts)
            {
                text += part.type + " low=" + (LowestPoint(part.colliderGroup != null ? part.colliderGroup.colliders : null, ground) * 100f).ToString("0", ci)
                    + " vy=" + (part.physicBody != null ? part.physicBody.velocity.y.ToString("0.00", ci) : "-") + "; ";
            }
            text += "| held " + HeldLow(creature.handLeft, "L", ground, ci) + HeldLow(creature.handRight, " R", ground, ci);
            var capsule = creature.locomotion.capsuleCollider;
            if (capsule != null)
            {
                text += " | capsule on=" + capsule.enabled + " bottom=" + ((capsule.bounds.min.y - ground) * 100f).ToString("0", ci)
                    + " height=" + capsule.height.ToString("0.00", ci) + " layer=" + UnityEngine.LayerMask.LayerToName(capsule.gameObject.layer);
            }
            return text;
        }

        private static string HeldLow(RagdollHand hand, string side, float ground, System.Globalization.CultureInfo ci)
        {
            var item = hand != null && hand.grabbedHandle != null ? hand.grabbedHandle.item : null;
            if (item == null)
            {
                return side + "=none";
            }
            var colliders = new List<UnityEngine.Collider>(item.GetComponentsInChildren<UnityEngine.Collider>());
            return side + "=" + item.data.id + " low=" + (LowestPoint(colliders, ground) * 100f).ToString("0", ci);
        }

        private static float LowestPoint(List<UnityEngine.Collider> colliders, float ground)
        {
            var lowest = float.PositiveInfinity;
            if (colliders != null)
            {
                foreach (var collider in colliders)
                {
                    if (collider != null && collider.enabled && !collider.isTrigger)
                    {
                        lowest = UnityEngine.Mathf.Min(lowest, collider.bounds.min.y - ground);
                    }
                }
            }
            return lowest;
        }

        private static string RootPin(Creature creature, System.Globalization.CultureInfo ci)
        {
            var root = creature.ragdoll.rootPart;
            if (root == null || root.bone == null || root.bone.animationJoint == null)
            {
                return "root pin none";
            }
            var drive = root.bone.animationJoint.yDrive;
            return "root pin spring=" + drive.positionSpring.ToString("0", ci)
                + " max=" + drive.maximumForce.ToString("0", ci)
                + " hipsAttached=" + creature.ragdoll.hipsAttached
                + " gravity=" + root.physicBody.useGravity
                + " kinematic=" + root.physicBody.isKinematic;
        }

        // Every collider straight below the hips down to the ground point, nearest first.
        private static string UnderHips(Creature creature, System.Globalization.CultureInfo ci)
        {
            var root = creature.ragdoll.rootPart;
            if (root == null)
            {
                return "-";
            }
            var origin = root.transform.position;
            var hits = UnityEngine.Physics.RaycastAll(origin, UnityEngine.Vector3.down, 1.5f, ~0, UnityEngine.QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            var text = string.Empty;
            foreach (var hit in hits)
            {
                text += hit.collider.name + "@" + hit.distance.ToString("0.00", ci)
                    + "[" + UnityEngine.LayerMask.LayerToName(hit.collider.gameObject.layer) + "] ";
            }
            return text.Length > 0 ? text : "nothing";
        }

        private static float AnimatedHeight(Creature creature, RagdollPart part)
        {
            return part != null && part.bone != null && part.bone.animation != null
                ? part.bone.animation.position.y - creature.transform.position.y
                : float.NaN;
        }

        // Physical / animated height of each foot above the creature's ground point.
        private static string FootHeights(Creature creature, System.Globalization.CultureInfo ci)
        {
            var text = string.Empty;
            foreach (var part in creature.ragdoll.parts)
            {
                if (part.type == RagdollPart.Type.LeftFoot || part.type == RagdollPart.Type.RightFoot)
                {
                    text += (part.type == RagdollPart.Type.LeftFoot ? "L" : " R")
                        + (part.transform.position.y - creature.transform.position.y).ToString("0.00", ci)
                        + "/" + AnimatedHeight(creature, part).ToString("0.00", ci);
                }
            }
            return text;
        }

        private static BrainModuleMelee Melee(Creature creature)
        {
            return creature.brain != null && creature.brain.instance != null
                ? creature.brain.instance.GetModule<BrainModuleMelee>(autoAddModuleIfMissing: false)
                : null;
        }

        private void StopCrawl(Creature creature)
        {
            lastPoseLog.Remove(creature);
            if (!crawling.Remove(creature))
            {
                return;
            }
            creature.SetAnimatorHeightRatio(1f);
            creature.animator.SetFloat(Brain.hashInjured, 0f);
            creature.locomotion.RemoveSpeedModifier(this);
            creature.ragdoll.forcePhysic.Remove(this);
            gaitMoving.Remove(creature);
            if (meleeBefore.TryGetValue(creature, out var enabled))
            {
                var melee = Melee(creature);
                if (melee != null)
                {
                    melee.meleeEnabled = enabled;
                }
                meleeBefore.Remove(creature);
            }
        }

        // The controller instance is the modifier key, so only its own hold is removed.
        // Explicit != null: ?. would skip Unity's destroyed-object check.
        private void Hold(Creature creature)
        {
            if (creature.brain != null)
            {
                creature.brain.AddNoStandUpModifier(this);
            }
        }

        private void Release(Creature creature)
        {
            if (creature.brain != null)
            {
                creature.brain.RemoveNoStandUpModifier(this);
            }
        }
    }
}
