using Hellscape.Domain;

namespace Hellscape.Tests {
    public class TestClock : IClock {
        public float FixedDelta => 0.02f; // 50Hz for deterministic testing
    }
}
