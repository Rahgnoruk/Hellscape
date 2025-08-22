using NUnit.Framework;
using Hellscape.Domain;

public class LifeSystem_Tests
{
    [Test]
    public void RespawnsAfter10sIfOthersAlive()
    {
        var life = new LifeSystem(10f);
        life.MarkDead(1);
        
        // one alive on team
        for (int i=0;i<10;i++) life.Tick(1f, alivePlayerCount: 1);
        
        var rs = life.ConsumeRespawns();
        Assert.AreEqual(1, rs.Count);
        Assert.AreEqual(1, rs[0]);
    }

    [Test]
    public void ResetsTimerIfAnotherDies()
    {
        var life = new LifeSystem(10f);
        life.MarkDead(1);
        life.Tick(5f, 1);
        
        // another player dies → timer restarts implicitly by MarkDead
        life.MarkDead(2);
        
        for (int i=0;i<9;i++) life.Tick(1f, 1);
        
        // not yet, needs full 10s since last death
        var rs = life.ConsumeRespawns();
        Assert.AreEqual(0, rs.Count);
        
        life.Tick(1f, 1);
        rs = life.ConsumeRespawns();
        Assert.AreEqual(2, rs.Count);
    }

    [Test]
    public void TeamWipeStopsCountdown()
    {
        var life = new LifeSystem(10f);
        life.MarkDead(1);
        
        // no alive players → countdown halts
        for (int i=0;i<20;i++) life.Tick(1f, alivePlayerCount: 0);
        
        var rs = life.ConsumeRespawns();
        Assert.AreEqual(0, rs.Count);
    }
}
