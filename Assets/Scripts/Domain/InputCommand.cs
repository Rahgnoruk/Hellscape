namespace Hellscape.Domain {
    public struct InputCommand {
        public int tick;
        public float moveX, moveY;
        public float aimX, aimY;
        public byte buttons;
        
        public InputCommand(int tick, float moveX, float moveY, float aimX, float aimY, byte buttons) {
            this.tick = tick;
            this.moveX = moveX;
            this.moveY = moveY;
            this.aimX = aimX;
            this.aimY = aimY;
            this.buttons = buttons;
        }
    }
}

