namespace Hellscape.Domain {
    public struct ActorState {
        public int id;
        public float positionX, positionY;
        public float velocityX, velocityY;
        public short hp;
        public byte type;
        public Team team;
        public float radius;
        public bool alive;
        
        public ActorState(int id, float positionX, float positionY, float velocityX, float velocityY, short hp, byte type, Team team, float radius, bool alive) {
            this.id = id;
            this.positionX = positionX;
            this.positionY = positionY;
            this.velocityX = velocityX;
            this.velocityY = velocityY;
            this.hp = hp;
            this.type = type;
            this.team = team;
            this.radius = radius;
            this.alive = alive;
        }
    }
}
