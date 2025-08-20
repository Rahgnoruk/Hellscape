using NUnit.Framework;
using Hellscape.Domain;

namespace Hellscape.Tests {
    [TestFixture]
    public class CityGridTests {
        
        [Test]
        public void FartherPointsAlwaysHaveBiggerRadiusIndex() {
            // Arrange
            var grid = new CityGrid(10, 10, 3);
            
            // Act & Assert - points further from center never have smaller radius index
            var centerRadius = grid.RadiusIndex(grid.CenterX, grid.CenterY);
            var adjacentRadius = grid.RadiusIndex(grid.CenterX + 1, grid.CenterY);
            var farRadius = grid.RadiusIndex(grid.CenterX + 3, grid.CenterY);
            
            Assert.That(centerRadius, Is.LessThanOrEqualTo(adjacentRadius));
            Assert.That(adjacentRadius, Is.LessThanOrEqualTo(farRadius));
        }
        
        [Test]
        public void RadiusIndexUsesChebyshevDistance() {
            // Arrange
            var grid = new CityGrid(10, 10, 3);
            
            // Act & Assert - Chebyshev distance should be max of abs differences
            var radius1 = grid.RadiusIndex(grid.CenterX + 2, grid.CenterY + 1);
            var radius2 = grid.RadiusIndex(grid.CenterX + 1, grid.CenterY + 2);
            
            // Both should have radius 2 (max of 2,1 and 1,2)
            Assert.That(radius1, Is.EqualTo(2));
            Assert.That(radius2, Is.EqualTo(2));
        }
        
        [Test]
        public void TileTagsByRadius() {
            // Arrange
            var grid = new CityGrid(10, 10, 3);
            
            // Act & Assert - tags should be assigned based on radius
            var downtownTag = grid.GetTag(grid.CenterX, grid.CenterY);
            var industrialTag = grid.GetTag(grid.CenterX + 2, grid.CenterY);
            var suburbsTag = grid.GetTag(grid.CenterX + 4, grid.CenterY);
            
            Assert.That(downtownTag, Is.EqualTo(TileTag.Downtown));
            Assert.That(industrialTag, Is.EqualTo(TileTag.Industrial));
            Assert.That(suburbsTag, Is.EqualTo(TileTag.Suburbs));
        }
    }
}
