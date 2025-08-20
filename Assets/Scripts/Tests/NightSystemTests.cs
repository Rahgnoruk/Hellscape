using NUnit.Framework;
using Hellscape.Domain;

namespace Hellscape.Tests {
    [TestFixture]
    public class NightSystemTests {
        [Test]
        public void NightSystemStartsNight0Time0InitialCorruption10()
        {
            // Arrange
            var nightSystem = new NightSystem();
            var initialNightCount = nightSystem.NightCount;
            var initialTimeOfDay = nightSystem.TimeOfDay;

            Assert.That(nightSystem.NightCount == 0, "Initial NightCount should be 0");
            Assert.That(nightSystem.TimeOfDay == 0f, "Initial TimeOfDay should be 0");
            Assert.That(nightSystem.CorruptionRadius == 10f, "Initial CorruptionRadius should be 10");
        }
        [Test]
        public void NightSystemAdvancesNight() {
            // Arrange
            var nightSystem = new NightSystem();
            var initialNightCount = nightSystem.NightCount;
            var initialTimeOfDay = nightSystem.TimeOfDay;
            
            // Act - advance past one day
            for (int i = 0; i < 301; i++) { // More than DayLengthSeconds (300)
                nightSystem.Tick(1f); // 1 second per tick
            }
            
            // Assert
            Assert.That(nightSystem.NightCount, Is.EqualTo(initialNightCount + 1));
            Assert.That(nightSystem.TimeOfDay, Is.GreaterThanOrEqualTo(0f));
            Assert.That(nightSystem.TimeOfDay, Is.LessThan(1f));
        }
        
        [Test]
        public void CorruptionRadiusGrows() {
            // Arrange
            var nightSystem = new NightSystem();
            var initialRadius = nightSystem.CorruptionRadius;
            
            // Act - advance to next night
            for (int i = 0; i < 301; i++) {
                nightSystem.Tick(1f);
            }
            
            // Assert
            Assert.That(nightSystem.CorruptionRadius, Is.GreaterThan(initialRadius));
        }
    }
}
