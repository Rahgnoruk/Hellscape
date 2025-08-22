using NUnit.Framework;
using Hellscape.Domain;

namespace Hellscape.Tests {
    public class BasicMovementTests {
        
        [Test]
        public void InputCommand_Creation_Works() {
            // Arrange & Act
            var command = new InputCommand(1, 1.0f, 0.5f, 0.0f, 0.0f, 0x04);
            
            // Assert
            Assert.That(command.tick, Is.EqualTo(1));
            Assert.That(command.moveX, Is.EqualTo(1.0f));
            Assert.That(command.moveY, Is.EqualTo(0.5f));
            Assert.That(command.buttons, Is.EqualTo(0x04));
        }
        
        [Test]
        public void ActorState_Creation_Works() {
            // Arrange & Act
            var state = new ActorState(1, 10.0f, 20.0f, 5.0f, 0.0f, 100, 0, Team.Player, 0.45f, true);
            
            // Assert
            Assert.That(state.id, Is.EqualTo(1));
            Assert.That(state.positionX, Is.EqualTo(10.0f));
            Assert.That(state.positionY, Is.EqualTo(20.0f));
            Assert.That(state.velocityX, Is.EqualTo(5.0f));
            Assert.That(state.hp, Is.EqualTo(100));
            Assert.That(state.type, Is.EqualTo(0));
            Assert.That(state.team, Is.EqualTo(Team.Player));
            Assert.That(state.radius, Is.EqualTo(0.45f));
            Assert.That(state.alive, Is.EqualTo(true));
        }
        
        [Test]
        public void MovementConstants_AreDefined() {
            // Assert
            Assert.That(MovementConstants.PlayerSpeed, Is.GreaterThan(0));
            Assert.That(MovementConstants.PlayerAcceleration, Is.GreaterThan(0));
            Assert.That(MovementConstants.DashImpulse, Is.GreaterThan(0));
            Assert.That(MovementConstants.DashButtonBit, Is.EqualTo(0x04));
            Assert.That(MovementConstants.AttackButtonBit, Is.EqualTo(0x01));
        }
        
        [Test]
        public void ServerSim_Creation_Works() {
            // Arrange & Act
            var sim = new ServerSim(42);
            
            // Assert
            Assert.That(sim, Is.Not.Null);
            Assert.That(sim.GetCurrentTick(), Is.EqualTo(0));
        }
    }
}
