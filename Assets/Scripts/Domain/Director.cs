namespace Hellscape.Domain {
    public sealed class Director {
        public float Intensity { get; private set; } = 0f;
        public void Tick()
        {
            if (Intensity > 0f) Intensity *= 0.98f; // cool down
        }
        public void Bump(float amount)
        {
            // Clamp to [0,1]
            float x = Intensity + amount;
            if (x < 0f) x = 0f; else if (x > 1f) x = 1f;
            Intensity = x;
        }
    }
}