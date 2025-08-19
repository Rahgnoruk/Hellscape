using UnityEngine;


namespace Hellscape.Presentation {
    public sealed class HitStop : MonoBehaviour {
        public static void Do(float duration=0.05f) { 
            instance?.StartCoroutine(instance.Co(duration)); 
        }
        private static HitStop instance; void Awake(){ 
            instance = this; 
        }
        private System.Collections.IEnumerator Co(float d){ 
            var t=Time.timeScale; Time.timeScale = 0.0f; 
            yield return new WaitForSecondsRealtime(d); 
            Time.timeScale = t; 
        }
    }
}