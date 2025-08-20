namespace Hellscape.Domain {
    public sealed class NightSystem {
        public int NightCount { get; private set; } = 0;
        public float TimeOfDay { get; private set; } = 0f; // 0..1
        public float DayLengthSeconds = 300f; // 5 minutes

        public void Tick(float deltaTime) {
            TimeOfDay += deltaTime / DayLengthSeconds;
            if (TimeOfDay >= 1f) { 
                TimeOfDay = 0f; 
                NightCount++; 
            }
        }

        public float CorruptionRadius => 10f + NightCount * 4f; // base + k * NightCount (strictly monotonic)
    }
}