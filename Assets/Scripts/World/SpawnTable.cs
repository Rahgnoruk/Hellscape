using UnityEngine;


namespace Hellscape.World {
    [CreateAssetMenu(menuName="Hellscape/SpawnTable")]
    public sealed class SpawnTable : ScriptableObject {
        [System.Serializable] public struct Entry { 
            public EnemyType Type; 
            public int MinRing; 
            public int MaxRing; 
            public int NightMin; 
            public float Weight; 
        }
        public Entry[] Entries;


        public float WeightFor(EnemyType t, int ring, int night) {
            float w = 0f;
            foreach (var e in Entries){
                if (e.Type==t && ring>=e.MinRing && ring<=e.MaxRing && night>=e.NightMin){
                    w += e.Weight;
                }
            } 
            return w;
        }
    }
}