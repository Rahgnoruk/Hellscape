using System.Collections.Generic;

namespace Hellscape.Domain {
    public sealed class EnemyDatabase {
        private readonly Dictionary<string, EnemyType> enemies = new();

        public void Register(EnemyType enemyType) {
            enemies[enemyType.Name] = enemyType;
        }

        public EnemyType Get(string name) {
            return enemies.TryGetValue(name, out var enemy) ? enemy : null;
        }
    }
}