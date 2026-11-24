namespace AchillesTendon.Logic
{
    /// <summary>One line of an NPC's voice, as BrainModuleSpeak.Play takes it.</summary>
    public struct VoiceLine
    {
        public string DialogId;
        public bool AbnormalNoise;
        public bool CheckMuffled;
    }

    /// <summary>
    /// The cry when the tendon is cut and the NPC goes down (Nadi, 2026-11-24: always the NPC's own
    /// death sound, no option; the pain scream "hit" sounded like any hit, the fall scream "falling"
    /// was too long and too loud). Picked as the game's BrainModuleSpeak.OnKillEvent picks a death
    /// sound for a cut (1.3.2): "muffleddeath" while a hand covers the mouth (played quietly and
    /// past the muffle), else "death". Every human voice in the 1.3.2 catalog has both
    /// (Voices/Voice_Human*.json, 16 voices).
    /// </summary>
    public static class CollapseVoiceRules
    {
        public const string DeathDialogId = "death";
        public const string MuffledDeathDialogId = "muffleddeath";

        public static VoiceLine CryFor(bool isMuffled)
        {
            return isMuffled
                ? new VoiceLine { DialogId = MuffledDeathDialogId, AbnormalNoise = false, CheckMuffled = false }
                : new VoiceLine { DialogId = DeathDialogId, AbnormalNoise = true, CheckMuffled = true };
        }
    }
}
