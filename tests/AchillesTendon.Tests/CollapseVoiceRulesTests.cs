using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class CollapseVoiceRulesTests
    {
        [Fact]
        public void Free_mouth_cries_the_death_sound_out_loud()
        {
            var line = CollapseVoiceRules.CryFor(isMuffled: false);

            Assert.Equal("death", line.DialogId);
            Assert.True(line.AbnormalNoise);
            Assert.True(line.CheckMuffled);
        }

        [Fact]
        public void Covered_mouth_gives_the_muffled_death_sound_past_the_muffle()
        {
            var line = CollapseVoiceRules.CryFor(isMuffled: true);

            Assert.Equal("muffleddeath", line.DialogId);
            Assert.False(line.AbnormalNoise);
            Assert.False(line.CheckMuffled);
        }
    }
}
