using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class ChanceTests
    {
        // roll is uniform in [0, 1] (UnityEngine.Random.value can return 1).
        [Theory]
        [InlineData(0.5f, 0.49f, true)]
        [InlineData(0.5f, 0.5f, false)]
        [InlineData(0f, 0f, false)]
        [InlineData(-1f, 0f, false)]
        [InlineData(1f, 1f, true)]
        [InlineData(2f, 1f, true)]
        public void Hits_below_the_chance(float chance, float roll, bool hit)
        {
            Assert.Equal(hit, Chance.Hits(chance, roll));
        }
    }
}
