namespace Hellscape.Domain {
    public static class MovementConstants {
        public const float PlayerSpeed = 5.0f;
        public const float PlayerAcceleration = 15.0f;
        public const float PlayerDeceleration = 20.0f;
        public const float DashImpulse = 8.0f;
        public const float DashCooldown = 0.5f;
        public const byte DashButtonBit = 0x04;
        public const byte AttackButtonBit = 0x01;
    }
}
