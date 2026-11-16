using AchillesTendon.Logic;
using Xunit;

namespace AchillesTendon.Tests
{
    public class HitLogFormatterTests
    {
        private static HitLogEntry Entry(HeelHitResult result)
        {
            return new HitLogEntry
            {
                Target = "HumanMale #1234",
                PartName = "LeftLeg",
                PartType = "LeftLeg",
                PartSection = "Lower",
                DamageType = "Slash",
                Damage = 12.345f,
                ImpactVelocityAngle = 33.33f,
                Result = result,
                ZoneRadius = 0.08f,
                MaxHitAngle = 110f,
            };
        }

        [Fact]
        public void Heel_hit_line_contains_part_angle_and_damage_type()
        {
            var result = new HeelHitResult
            {
                Rejection = HeelHitRejection.None,
                HasZone = true,
                DistanceToZoneCenter = 0.0512f,
                AttackAngle = 34.04f,
            };

            var line = HitLogFormatter.Format(Entry(result));

            Assert.Equal(
                "HEEL HIT | target=HumanMale #1234 | part=LeftLeg type=LeftLeg section=Lower | damage=Slash 12.35"
                + " | zone dist=0.051m r=0.080m | angleFromBehind=34.0° max=110° | impactVelAngle=33.3°",
                line);
        }

        [Fact]
        public void Rejected_hit_names_the_reason_and_marks_unknown_values()
        {
            var result = new HeelHitResult
            {
                Rejection = HeelHitRejection.NoAnkle,
                HasZone = false,
                DistanceToZoneCenter = float.NaN,
                AttackAngle = float.NaN,
            };
            var entry = Entry(result);
            entry.ImpactVelocityAngle = float.NaN;

            var line = HitLogFormatter.Format(entry);

            Assert.StartsWith("miss (NoAnkle) | ", line);
            Assert.Contains("zone dist=n/a r=0.080m", line);
            Assert.Contains("angleFromBehind=n/a max=110°", line);
            Assert.EndsWith("impactVelAngle=n/a", line);
        }
    }
}
