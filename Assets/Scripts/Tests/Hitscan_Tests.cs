using NUnit.Framework;
using Hellscape.Domain;
using Hellscape.Domain.Combat;

public class Hitscan_Tests
{
    [Test] public void Hits_Rightward_Line()
    {
        var a = new Vector2(0,0);
        var b = new Vector2(10,0);
        var c = new Vector2(5,0);
        Assert.IsTrue(Hitscan.SegmentCircle(a,b,c,0.5f, out var t));
        Assert.That(t, Is.InRange(0.0f, 1.0f));
    }

    [Test] public void Hits_Leftward_Line()
    {
        var a = new Vector2(10,0);
        var b = new Vector2(0,0);  // reversed direction
        var c = new Vector2(5,0);
        Assert.IsTrue(Hitscan.SegmentCircle(a,b,c,0.5f, out var t));
        Assert.That(t, Is.InRange(0.0f, 1.0f));
    }

    [Test] public void Misses_When_Outside_Radius()
    {
        var a = new Vector2(0,0);
        var b = new Vector2(10,0);
        var c = new Vector2(5,1.0f);
        Assert.IsFalse(Hitscan.SegmentCircle(a,b,c,0.4f, out var _));
    }
}
