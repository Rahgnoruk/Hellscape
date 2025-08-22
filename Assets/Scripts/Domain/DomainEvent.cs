namespace Hellscape.Domain {
    public struct DomainEvent {
        public enum Kind {
            HitLanded,
            ActorDied
        }
        
        public Kind kind;
        public int attackerId;
        public int targetId;
        public float damage;
        
        public static DomainEvent HitLanded(int attackerId, int targetId, float damage) {
            return new DomainEvent {
                kind = Kind.HitLanded,
                attackerId = attackerId,
                targetId = targetId,
                damage = damage
            };
        }
        
        public static DomainEvent ActorDied(int targetId) {
            return new DomainEvent {
                kind = Kind.ActorDied,
                targetId = targetId
            };
        }
    }
}
