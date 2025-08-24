namespace Hellscape.Domain {
    public sealed class DeterministicRng : IRng {
        private uint state;
        
        public DeterministicRng(int seed) { 
            state = (uint)seed; 
            if (state == 0) {
                state = 1; 
            }
        }
        
        // Xorshift32
        public uint NextU() { 
            uint x = state; 
            x ^= x << 13; 
            x ^= x >> 17; 
            x ^= x << 5; 
            state = x; 
            return x; 
        }
        
        public float Next01() => (NextU() & 0x00FFFFFF) / 16777216f; // 24-bit
        
        public int Range(int minInclusive, int maxExclusive) {
            var u = NextU();
            var span = (uint)(maxExclusive - minInclusive);
            return (int)(u % span) + minInclusive;
        }
        
        public float RangeFloat(float minInclusive, float maxInclusive) {
            var t = Next01(); // 0..1
            return minInclusive + t * (maxInclusive - minInclusive);
        }
    }
}