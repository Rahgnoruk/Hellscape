using UnityEngine;


namespace Hellscape.World {
    [CreateAssetMenu(menuName="Hellscape/EnemyDatabase")]
    public sealed class EnemyDatabase : ScriptableObject {
        public EnemyType[] Types;
    }
}