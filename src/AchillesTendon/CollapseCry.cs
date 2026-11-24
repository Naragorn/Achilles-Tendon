using System.Collections.Generic;
using AchillesTendon.Logic;
using ThunderRoad;

namespace AchillesTendon
{
    /// <summary>
    /// The NPC cries out with its own death sound when its tendon is cut and it goes down
    /// (Nadi, 2026-11-24: always, no option; the dialog is picked in CollapseVoiceRules).
    ///
    /// Played in onCreatureHit at the hit's start, before the game's damage. The game's own pain
    /// sound for that hit (BrainModuleSpeak.OnDamageEvent, "hit") is skipped while the voice is
    /// still playing (read in 1.3.2), so the cry is not cut off by it; the first ground-fight
    /// scream waits one Scream Interval (GroundFightController.Start).
    ///
    /// With Debug Logging on, every line the voice tries to play in the next few seconds is logged
    /// (VOICE lines, from BrainModuleSpeak.OnTrySpeak), so a log shows what was heard after the cut.
    /// </summary>
    internal sealed class CollapseCry
    {
        private const float WatchSeconds = 4f;

        private sealed class Watch
        {
            public BrainModuleSpeak Speak;
            public BrainModuleSpeak.TrySpeak Handler;
            public float Until;
        }

        private readonly Dictionary<Creature, Watch> watches = new Dictionary<Creature, Watch>();
        private readonly List<Creature> scratch = new List<Creature>();

        public void Play(Creature creature, float time)
        {
            var speak = creature.brain != null && creature.brain.instance != null
                ? creature.brain.instance.GetModule<BrainModuleSpeak>(autoAddModuleIfMissing: false)
                : null;
            if (speak == null)
            {
                ModLog.Verbose("COLLAPSE CRY | " + GameAdapters.Label(creature) + " | no voice");
                return;
            }
            var line = CollapseVoiceRules.CryFor(creature.brain.isMuffled);
            if (ModOptions.DebugLogging)
            {
                StartWatch(creature, speak, time);
            }
            speak.Play(line.DialogId, line.AbnormalNoise, line.CheckMuffled);
            ModLog.Verbose("COLLAPSE CRY | " + GameAdapters.Label(creature) + " | dialog=" + line.DialogId);
        }

        public void Update(float time)
        {
            if (watches.Count == 0)
            {
                return;
            }
            scratch.Clear();
            foreach (var pair in watches)
            {
                if (pair.Key == null || time >= pair.Value.Until)
                {
                    scratch.Add(pair.Key);
                }
            }
            foreach (var creature in scratch)
            {
                StopWatch(creature);
            }
        }

        public void Clear()
        {
            scratch.Clear();
            scratch.AddRange(watches.Keys);
            foreach (var creature in scratch)
            {
                StopWatch(creature);
            }
            watches.Clear();
        }

        private void StartWatch(Creature creature, BrainModuleSpeak speak, float time)
        {
            if (watches.TryGetValue(creature, out var running))
            {
                running.Until = time + WatchSeconds;
                return;
            }
            var label = GameAdapters.Label(creature);
            BrainModuleSpeak.TrySpeak handler = (abnormal, dialogHash, dialog, clip, clipHash, status) =>
                ModLog.Verbose("VOICE | " + label + " | dialog=" + (dialog != null ? dialog.id : dialogHash.ToString())
                    + " | " + status + " | clip=" + (clip != null ? clip.name + " (" + clip.length.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + " s)" : "-"));
            speak.OnTrySpeak += handler;
            watches[creature] = new Watch { Speak = speak, Handler = handler, Until = time + WatchSeconds };
        }

        private void StopWatch(Creature creature)
        {
            if (watches.TryGetValue(creature, out var watch))
            {
                if (watch.Speak != null)
                {
                    watch.Speak.OnTrySpeak -= watch.Handler;
                }
                watches.Remove(creature);
            }
        }
    }
}
