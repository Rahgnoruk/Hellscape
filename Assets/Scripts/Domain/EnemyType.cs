using System;

namespace Hellscape.Domain {
    public sealed class EnemyType {
        public readonly string Name;
        public readonly int MaxHp;
        public readonly float MoveSpeed;
        public readonly float AttackRange;
        public readonly float AttackDamage;
        public readonly float AttackCooldown;

        public EnemyType(string name, int maxHp, float moveSpeed, float attackRange, float attackDamage, float attackCooldown) {
            Name = name;
            MaxHp = maxHp;
            MoveSpeed = moveSpeed;
            AttackRange = attackRange;
            AttackDamage = attackDamage;
            AttackCooldown = attackCooldown;
        }
    }
}