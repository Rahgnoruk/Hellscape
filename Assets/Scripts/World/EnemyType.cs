using UnityEngine;


namespace Hellscape.World {
    public enum EnemyTag { Fast, Armored, Meaty, Ranged, Swarm, Poison, Screamer, Jumper, Berserk }


    [CreateAssetMenu(menuName="Hellscape/EnemyType")]
    public sealed class EnemyType : ScriptableObject {
        public string Id;
        public EnemyTag Tag;
        [Header("TTK tuning (solo baseline)")] public float EHP = 100f; // effective HP baseline
        public float MoveSpeed = 3f;
        public bool HasRanged;
        public float NoiseOnAction = 0.5f;
        [TextArea] public string Notes;
    }
}