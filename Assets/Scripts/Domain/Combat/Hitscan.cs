namespace Hellscape.Domain.Combat
{
    // Lightweight vector helpers for Domain.Vector2
    internal static class V2
    {
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 Sub(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 Add(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 Mul(Vector2 a, float k) => new Vector2(a.x * k, a.y * k);
        public static float LenSq(Vector2 a) => Dot(a, a);
    }

    public static class Hitscan
    {
        // Returns true if the circle intersects the line.
        // Out linePercentClosestToCicleCenter is clamped to [0,1] and is the closest point on the line to the circle's center.
        public static bool SegmentCircle(Vector2 lineStart, Vector2 lineEnd, Vector2 center, float radius, out float linePercentClosestToCicleCenter)
        {
            var line = V2.Sub(lineEnd, lineStart);
            var startToCenter = V2.Sub(center, lineStart);
            var lineLenSq = V2.LenSq(line);
            if (lineLenSq <= 1e-8f) {
                linePercentClosestToCicleCenter = 0f;
                return V2.LenSq(startToCenter) <= radius * radius;
            }

            linePercentClosestToCicleCenter = V2.Dot(startToCenter, line) / lineLenSq; // projection parameter (can be <0 or >1)
            if (linePercentClosestToCicleCenter < 0f) linePercentClosestToCicleCenter = 0f;
            else if (linePercentClosestToCicleCenter > 1f) linePercentClosestToCicleCenter = 1f;

            var closest = V2.Add(lineStart, V2.Mul(line, linePercentClosestToCicleCenter));
            var distSq = V2.LenSq(V2.Sub(center, closest));
            return distSq <= radius * radius;
        }
    }

    // Domain-level fired shot event. Read by App after Tick and cleared.
    public struct ShotEvent
    {
        public Vector2 start, end;
        public bool hit; // optional
        public ShotEvent(Vector2 s, Vector2 e, bool h) {
            start = s;
            end = e;
            hit = h;
        }
    }
}
