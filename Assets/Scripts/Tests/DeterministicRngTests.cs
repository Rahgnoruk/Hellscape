using NUnit.Framework;
using Hellscape.Domain;

namespace Hellscape.Tests {
    [TestFixture]
    public class DeterministicRngTests {

        [Test]
        public void SameSeedProducesSameSequences()
        {
            var rng1 = new DeterministicRng(42);
            var rng2 = new DeterministicRng(42);

            // Act
            var values1 = new float[10];
            var values2 = new float[10];

            for (int i = 0; i < 10; i++)
            {
                values1[i] = rng1.Next01();
                values2[i] = rng2.Next01();
            }

            // Assert - same seed produces same sequence
            Assert.That(values1, Is.EqualTo(values2));
        }

        [Test]
        public void DifferentSeedProducesDifferentSequences() {
            // Arrange
            var rng1 = new DeterministicRng(42);
            var rng3 = new DeterministicRng(43);
            
            // Act
            var values1 = new float[10];
            var values2 = new float[10];
            
            for (int i = 0; i < 10; i++) {
                values1[i] = rng1.Next01();
                values2[i] = rng3.Next01();
            }
            
            // Assert - different seed produces different sequence
            Assert.That(values1, Is.Not.EqualTo(values2));
        }
        
        [Test]
        public void Next01ReturnsValidRange() {
            // Arrange
            var rng = new DeterministicRng(123);
            
            // Act & Assert
            for (int i = 0; i < 1000; i++) {
                var value = rng.Next01();
                Assert.That(value, Is.GreaterThanOrEqualTo(0f));
                Assert.That(value, Is.LessThan(1f));
            }
        }
        
        [Test]
        public void RangeReturnsValidValues() {
            // Arrange
            var rng = new DeterministicRng(456);
            
            // Act & Assert
            for (int i = 0; i < 1000; i++) {
                var value = rng.Range(10, 20);
                Assert.That(value, Is.GreaterThanOrEqualTo(10));
                Assert.That(value, Is.LessThan(20));
            }
        }
    }
}
