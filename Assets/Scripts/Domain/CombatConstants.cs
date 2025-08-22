namespace Hellscape.Domain {
    public static class CombatConstants {
        // Weapon parameters
        public const float PistolDamage = 25f;
        public const float PistolRange = 12f;
        public const int PistolCooldownTicks = 8; // @50Hz ≈ 0.16s
        
        // Enemy parameters
        public const float EnemySpeed = 3.5f;
        public const float EnemySenseRange = 50f;
        public const float EnemyRadius = 0.5f;
        public const float PlayerRadius = 0.45f;
    }
}
