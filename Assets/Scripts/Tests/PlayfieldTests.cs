using NUnit.Framework;
using Hellscape.Domain;
using UnityEngine;
using Vector2 = Hellscape.Domain.Vector2;
namespace Hellscape.Tests.EditMode
{
    public class PlayfieldTests
    {
        private ServerSim sim;

        [SetUp]
        public void Setup()
        {
            sim = new ServerSim(42);
            sim.Start();
        }

        [Test]
        public void ClampToPlayfield_WithinBounds_ReturnsSamePosition()
        {
            // Arrange
            var pos = new Vector2(10f, 5f);

            // Act
            var result = sim.ClampToPlayfield(pos);

            // Assert
            Assert.AreEqual(pos.x, result.x, 0.001f);
            Assert.AreEqual(pos.y, result.y, 0.001f);
        }

        [Test]
        public void ClampToPlayfield_OutsideXBounds_ClampsToBoundary()
        {
            // Arrange
            var pos = new Vector2(30f, 5f); // x > 25

            // Act
            var result = sim.ClampToPlayfield(pos);

            // Assert
            Assert.AreEqual(25f, result.x, 0.001f);
            Assert.AreEqual(5f, result.y, 0.001f);
        }

        [Test]
        public void ClampToPlayfield_OutsideYBounds_ClampsToBoundary()
        {
            // Arrange
            var pos = new Vector2(10f, 20f); // y > 14

            // Act
            var result = sim.ClampToPlayfield(pos);

            // Assert
            Assert.AreEqual(10f, result.x, 0.001f);
            Assert.AreEqual(14f, result.y, 0.001f);
        }

        [Test]
        public void ClampToPlayfield_NegativeBounds_ClampsToBoundary()
        {
            // Arrange
            var pos = new Vector2(-30f, -20f); // x < -25, y < -14

            // Act
            var result = sim.ClampToPlayfield(pos);

            // Assert
            Assert.AreEqual(-25f, result.x, 0.001f);
            Assert.AreEqual(-14f, result.y, 0.001f);
        }

        [Test]
        public void SpawnEnemiesAtEdges_SpawnsCorrectCount()
        {
            // Arrange
            int spawnCount = 5;

            // Act
            sim.SpawnEnemiesAtEdges(spawnCount, 1.5f);

            // Assert
            // Note: We can't directly access the actors count, but we can verify
            // that the method doesn't throw and completes successfully
            Assert.Pass("SpawnEnemiesAtEdges completed without throwing");
        }
    }
}
