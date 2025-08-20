namespace Hellscape.World {
    public sealed class Director {
        public float Intensity { get; private set; } // 0..1
        public void Tick(){
            // TODO: compute based on damage taken, ammo state, time since last spike
            Intensity *= 0.98f; // cool down
        }
    }
}