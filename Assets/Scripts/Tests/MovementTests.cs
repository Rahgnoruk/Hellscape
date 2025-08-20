using NUnit.Framework;
using Hellscape.Domain;

namespace Hellscape.Tests {
    public class MovementTests {
        private ServerSim sim;
        private const float FixedDelta = 0.02f;
        
        [SetUp]
        public void Setup() {
            sim = new ServerSim(42);
            sim.Start();
        }
        
        [Test]
        public void Movement_ZeroInput_NoDrift() {
            // Arrange
            var initialSnapshot = sim.CreateSnapshot();
            var initialPos = initialSnapshot.actors[0]; // Player is first
            
            // Act - Apply zero input for multiple ticks
            for (int i = 0; i < 10; i++) {
                var zeroCommand = new InputCommand(sim.GetCurrentTick(), 0, 0, 0, 0, 0);
                sim.Apply(zeroCommand);
                sim.Tick(FixedDelta);
            }
            
            // Assert
            var finalSnapshot = sim.CreateSnapshot();
            var finalPos = finalSnapshot.actors[0];
            
            Assert.That(finalPos.x, Is.EqualTo(initialPos.x).Within(0.001f), "X position should not drift");
            Assert.That(finalPos.y, Is.EqualTo(initialPos.y).Within(0.001f), "Y position should not drift");
        }
        
        [Test]
        public void Movement_Forward_AdvancesPredictably() {
            // Arrange
            var initialSnapshot = sim.CreateSnapshot();
            var initialPos = initialSnapshot.actors[0];
            
            // Act - Apply forward movement for multiple ticks
            for (int i = 0; i < 10; i++) {
                var forwardCommand = new InputCommand(sim.GetCurrentTick(), 1, 0, 0, 0, 0);
                sim.Apply(forwardCommand);
                sim.Tick(FixedDelta);
            }
            
            // Assert
            var finalSnapshot = sim.CreateSnapshot();
            var finalPos = finalSnapshot.actors[0];
            
            // Should have moved in X direction (allowing for acceleration)
            Assert.That(finalPos.x, Is.GreaterThan(initialPos.x), "Should move forward in X direction");
            Assert.That(finalPos.y, Is.EqualTo(initialPos.y).Within(0.001f), "Should not move in Y direction");
        }

        [Test]
        public void Movement_Dash_Impulse()
        {
            // Arrange
            var initialSnapshot = sim.CreateSnapshot();
            var initialPos = initialSnapshot.actors[0];

            // Act - Apply dash command
            var dashCommand = new InputCommand(sim.GetCurrentTick(), 1, 0, 0, 0, MovementConstants.DashButtonBit);
            sim.Apply(dashCommand);
            sim.Tick(FixedDelta);

            // Assert - Should have moved significantly
            var afterDashSnapshot = sim.CreateSnapshot();
            var afterDashPos = afterDashSnapshot.actors[0];
            var dashDistance = afterDashPos.x - initialPos.x;
            Assert.That(dashDistance, Is.GreaterThan(0.1f), "Dash should provide significant movement");
        }
        
        [Test]
        public void Movement_AttackButton_DoesNotAffectMovement()
        {
            // Arrange
            var initialSnapshot = sim.CreateSnapshot();
            var initialPos = initialSnapshot.actors[0];

            // Act - Apply attack command (should not affect movement)
            var attackCommand = new InputCommand(sim.GetCurrentTick(), 0, 0, 0, 0, MovementConstants.AttackButtonBit);
            sim.Apply(attackCommand);
            sim.Tick(FixedDelta);

            // Assert - Position should remain the same
            var afterAttackSnapshot = sim.CreateSnapshot();
            var afterAttackPos = afterAttackSnapshot.actors[0];
            Assert.That(afterAttackPos.x, Is.EqualTo(initialPos.x).Within(0.001f), "Attack button should not affect movement");
            Assert.That(afterAttackPos.y, Is.EqualTo(initialPos.y).Within(0.001f), "Attack button should not affect movement");
        }

        [Test]
        public void Movement_Dash_Cooldown()
        {
            // Arrange
            var initialSnapshot = sim.CreateSnapshot();
            var initialPos = initialSnapshot.actors[0];

            // Act - Apply dash command
            var dashCommand = new InputCommand(sim.GetCurrentTick(), 1, 0, 0, 0, MovementConstants.DashButtonBit);
            sim.Apply(dashCommand);
            sim.Tick(FixedDelta);

            // Act - Try to dash again immediately (should be ignored due to cooldown)
            var secondDashCommand = new InputCommand(sim.GetCurrentTick(), 1, 0, 0, 0, MovementConstants.DashButtonBit);
            sim.Apply(secondDashCommand);
            sim.Tick(FixedDelta);

            // Assert - Should not have moved much more
            var afterDashSnapshot = sim.CreateSnapshot();
            var afterDashPos = afterDashSnapshot.actors[0];
            var afterSecondDashSnapshot = sim.CreateSnapshot();
            var afterSecondDashPos = afterSecondDashSnapshot.actors[0];
            var secondDashDistance = afterSecondDashPos.x - afterDashPos.x;
            Assert.That(secondDashDistance, Is.LessThan(0.1f), "Second dash should be ignored due to cooldown");
        }
        
        [Test]
        public void Snapshot_Roundtrip_Equal() {
            // Arrange
            var originalSnapshot = sim.CreateSnapshot();
            
            // Act
            var encoded = SnapshotCodec.Encode(originalSnapshot);
            var decoded = SnapshotCodec.Decode(encoded);
            
            // Assert
            Assert.That(decoded.tick, Is.EqualTo(originalSnapshot.tick), "Tick should match");
            Assert.That(decoded.actors.Length, Is.EqualTo(originalSnapshot.actors.Length), "Actor count should match");
            
            for (int i = 0; i < decoded.actors.Length; i++) {
                var original = originalSnapshot.actors[i];
                var decoded_actor = decoded.actors[i];
                
                Assert.That(decoded_actor.id, Is.EqualTo(original.id), $"Actor {i} ID should match");
                Assert.That(decoded_actor.x, Is.EqualTo(original.x).Within(0.001f), $"Actor {i} X should match");
                Assert.That(decoded_actor.y, Is.EqualTo(original.y).Within(0.001f), $"Actor {i} Y should match");
                Assert.That(decoded_actor.vx, Is.EqualTo(original.vx).Within(0.001f), $"Actor {i} VX should match");
                Assert.That(decoded_actor.vy, Is.EqualTo(original.vy).Within(0.001f), $"Actor {i} VY should match");
                Assert.That(decoded_actor.hp, Is.EqualTo(original.hp), $"Actor {i} HP should match");
                Assert.That(decoded_actor.type, Is.EqualTo(original.type), $"Actor {i} type should match");
            }
        }
        
        [Test]
        public void Determinism_SameCommandsSameSeed_SameFinalState() {
            // Arrange
            var sim1 = new ServerSim(42);
            var sim2 = new ServerSim(42);
            sim1.Start();
            sim2.Start();
            
            // Act - Apply same sequence of commands to both sims
            for (int i = 0; i < 20; i++) {
                var command = new InputCommand(i + 1, 1, 0, 0, 0, i % 10 == 0 ? MovementConstants.DashButtonBit : (byte)0);
                sim1.Apply(command);
                sim2.Apply(command);
                sim1.Tick(FixedDelta);
                sim2.Tick(FixedDelta);
            }
            
            // Assert - Final snapshots should be identical
            var snapshot1 = sim1.CreateSnapshot();
            var snapshot2 = sim2.CreateSnapshot();
            
            var encoded1 = SnapshotCodec.Encode(snapshot1);
            var encoded2 = SnapshotCodec.Encode(snapshot2);
            
            Assert.That(encoded1.Length, Is.EqualTo(encoded2.Length), "Encoded snapshots should have same length");
            for (int i = 0; i < encoded1.Length; i++) {
                Assert.That(encoded1[i], Is.EqualTo(encoded2[i]), $"Byte {i} should match");
            }
        }
    }
}
