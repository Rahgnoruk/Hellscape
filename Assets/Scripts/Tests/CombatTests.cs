using NUnit.Framework;
using Hellscape.Domain;

namespace Hellscape.Tests {
    public class CombatTests {
        private ServerSim sim;
        private TestClock clock;
        
        [SetUp]
        public void Setup() {
            sim = new ServerSim(42);
            sim.Start();
            clock = new TestClock();
        }
        
        [Test]
        public void Pistol_FirstEnemyAlongRay_TakesDamage() {
            // Arrange
            int playerId = sim.SpawnPlayerAt(new Vector2(0, 0));
            int enemyId = sim.SpawnEnemyAt(new Vector2(6, 0));
            
            var attackCommand = new InputCommand(1, 0, 0, 1, 0, MovementConstants.AttackButtonBit);
            
            // Act
            sim.ApplyForActor(playerId, attackCommand);
            sim.Tick(clock.FixedDelta);
            
            // Assert
            Assert.That(sim.TryGetActorState(enemyId, out var enemyState), Is.True);
            Assert.That(enemyState.hp, Is.EqualTo(75)); // 100 - 25 damage
        }
        
        [Test]
        public void Pistol_Cooldown_BlocksRapidFire() {
            // Arrange
            int playerId = sim.SpawnPlayerAt(new Vector2(0, 0));
            int enemyId = sim.SpawnEnemyAt(new Vector2(6, 0));
            
            var attackCommand = new InputCommand(1, 0, 0, 1, 0, MovementConstants.AttackButtonBit);
            
            // Act - First shot
            sim.ApplyForActor(playerId, attackCommand);
            sim.Tick(clock.FixedDelta);
            
            // Second shot immediately
            sim.ApplyForActor(playerId, attackCommand);
            sim.Tick(clock.FixedDelta);
            
            // Assert - Only first shot should hit
            Assert.That(sim.TryGetActorState(enemyId, out var enemyState), Is.True);
            Assert.That(enemyState.hp, Is.EqualTo(75)); // Only 25 damage, not 50
        }
        
        [Test]
        public void Pistol_RangeAndMiss_NoDamage() {
            // Arrange - Enemy out of range
            int playerId = sim.SpawnPlayerAt(new Vector2(0, 0));
            int enemyId = sim.SpawnEnemyAt(new Vector2(13, 0)); // > 12 range
            
            var attackCommand = new InputCommand(1, 0, 0, 1, 0, MovementConstants.AttackButtonBit);
            
            // Act
            sim.ApplyForActor(playerId, attackCommand);
            sim.Tick(clock.FixedDelta);
            
            // Assert
            Assert.That(sim.TryGetActorState(enemyId, out var enemyState), Is.True);
            Assert.That(enemyState.hp, Is.EqualTo(100)); // No damage
        }
        
        [Test]
        public void Pistol_MissByRadius_NoDamage() {
            // Arrange - Enemy outside aim radius
            int playerId = sim.SpawnPlayerAt(new Vector2(0, 0));
            int enemyId = sim.SpawnEnemyAt(new Vector2(5, 2)); // Off to the side
            
            var attackCommand = new InputCommand(1, 0, 0, 1, 0, MovementConstants.AttackButtonBit);
            
            // Act
            sim.ApplyForActor(playerId, attackCommand);
            sim.Tick(clock.FixedDelta);
            
            // Assert
            Assert.That(sim.TryGetActorState(enemyId, out var enemyState), Is.True);
            Assert.That(enemyState.hp, Is.EqualTo(100)); // No damage
        }
        
        [Test]
        public void Enemy_Chaser_MovesTowardPlayer() {
            // Arrange
            int playerId = sim.SpawnPlayerAt(new Vector2(0, 0));
            int enemyId = sim.SpawnEnemyAt(new Vector2(5, 0));
            
            var initialEnemyState = sim.TryGetActorState(enemyId, out var initialState) ? initialState : default;
            
            // Act - Let enemy AI run for several ticks
            for (int i = 0; i < 10; i++) {
                sim.Tick(clock.FixedDelta);
            }
            
            // Assert
            Assert.That(sim.TryGetActorState(enemyId, out var finalState), Is.True);
            Assert.That(finalState.positionX, Is.LessThan(initialState.positionX)); // Moved toward player
        }
        
        [Test]
        public void Death_EmitsActorDiedAndRemoves() {
            // Arrange - Enemy with low HP
            int playerId = sim.SpawnPlayerAt(new Vector2(0, 0));
            int enemyId = sim.SpawnEnemyAt(new Vector2(6, 0));
            
            // Reduce enemy HP to 20 (will die from 25 damage shot)
            sim.SetActorHp(enemyId, 20);
            
            var attackCommand = new InputCommand(1, 0, 0, 1, 0, MovementConstants.AttackButtonBit);
            
            // Act
            sim.ApplyForActor(playerId, attackCommand);
            sim.Tick(clock.FixedDelta);
            
            // Assert
            Assert.That(sim.TryGetActorState(enemyId, out var _), Is.False); // Enemy removed
        }
    }
}
