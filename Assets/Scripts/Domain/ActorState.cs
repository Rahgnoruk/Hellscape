namespace Hellscape.Domain {
    public struct ActorState {
        public int id;
        public float positionX, positionY;
        public float velocityX, velocityY;
        public short hp;
        public byte type;
        
        public ActorState(int id, float positionX, float positionY, float velocityX, float velocityY, short hp, byte type) {
            this.id = id;
            this.positionX = positionX;
            this.positionY = positionY;
            this.velocityX = velocityX;
            this.velocityY = velocityY;
            this.hp = hp;
            this.type = type;
        }
    }
}
