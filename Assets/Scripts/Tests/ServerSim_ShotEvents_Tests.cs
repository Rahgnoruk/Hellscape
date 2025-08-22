using NUnit.Framework;
using Hellscape.Domain;
using Hellscape.Domain.Combat;

public class ServerSim_ShotEvents_Tests
{
    [Test] public void ShotEvents_Generated_When_Player_Fires()
    {
        var sim = new ServerSim(42);
        sim.Start();
        
        // Spawn a player and enemy
        var playerId = sim.SpawnPlayerAt(new Vector2(0, 0));
        var enemyId = sim.SpawnEnemyAt(new Vector2(5, 0));
        
        // Submit input with attack button pressed and aim direction
        var cmd = new InputCommand(
            tick: 0,
            moveX: 0, moveY: 0,
            aimX: 1, aimY: 0, // aim right
            buttons: MovementConstants.AttackButtonBit
        );
        
        sim.ApplyForActor(playerId, cmd);
        sim.Tick(0.016f); // one frame
        
        // Check that shot events were generated
        var shotEvents = sim.ConsumeShotEvents();
        Assert.That(shotEvents.Count, Is.GreaterThan(0));
        
        // Verify the shot event has correct data
        var shot = shotEvents[0];
        Assert.That(shot.start.x, Is.EqualTo(0).Within(0.01f));
        Assert.That(shot.start.y, Is.EqualTo(0).Within(0.01f));
        Assert.That(shot.didHit, Is.True); // should hit the enemy at (5,0)
    }
    
    [Test] public void ShotEvents_Consumed_After_Reading()
    {
        var sim = new ServerSim(42);
        sim.Start();
        
        var playerId = sim.SpawnPlayerAt(new Vector2(0, 0));
        var enemyId = sim.SpawnEnemyAt(new Vector2(5, 0));
        
        var cmd = new InputCommand(
            tick: 0,
            moveX: 0, moveY: 0,
            aimX: 1, aimY: 0,
            buttons: MovementConstants.AttackButtonBit
        );
        
        sim.ApplyForActor(playerId, cmd);
        sim.Tick(0.016f);
        
        // First read should have events
        var firstRead = sim.ConsumeShotEvents();
        Assert.That(firstRead.Count, Is.GreaterThan(0));
        
        // Second read should be empty (consumed)
        var secondRead = sim.ConsumeShotEvents();
        Assert.That(secondRead.Count, Is.EqualTo(0));
    }
}
