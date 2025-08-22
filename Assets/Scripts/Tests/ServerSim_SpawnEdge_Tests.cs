using NUnit.Framework;
using Hellscape.Domain;

public class ServerSim_SpawnEdge_Tests
{
    [Test]
    public void EdgeSpawn_IsOnBoundsOrInside()
    {
        var sim = new ServerSim(seed:1234);
        sim.Start();
        var p = sim.GetRandomEdgePositionForBridge(1.5f);
        
        // crude bounds check against default 25x14 half-extents
        Assert.IsTrue(p.x <= 25f && p.x >= -25f);
        Assert.IsTrue(p.y <= 14f && p.y >= -14f);
        
        // and at least close to an edge (within ~1.6 of any side)
        bool nearX = (25f - System.Math.Abs(p.x)) <= 1.6f;
        bool nearY = (14f - System.Math.Abs(p.y)) <= 1.6f;
        Assert.IsTrue(nearX || nearY);
    }
}
