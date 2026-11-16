using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class BleedProgressTests
    {
        [Theory]
        [InlineData(0f, 45f, 0f)]
        [InlineData(22.5f, 45f, 0.5f)]
        [InlineData(45f, 45f, 1f)]
        [InlineData(60f, 45f, 1f)]
        public void Progress_is_elapsed_over_duration_clamped(float elapsed, float duration, float expected)
        {
            Assert.Equal(expected, BleedOutRules.Progress(elapsed, duration), 4);
        }

        [Fact]
        public void Zero_duration_counts_as_done()
        {
            Assert.Equal(1f, BleedOutRules.Progress(0f, 0f));
        }
    }
}
