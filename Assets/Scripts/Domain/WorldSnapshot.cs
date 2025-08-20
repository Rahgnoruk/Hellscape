namespace Hellscape.Domain {
    public struct WorldSnapshot {
        public int tick;
        public ActorState[] actors;
        
        public WorldSnapshot(int tick, ActorState[] actors) {
            this.tick = tick;
            this.actors = actors;
        }
    }
}
