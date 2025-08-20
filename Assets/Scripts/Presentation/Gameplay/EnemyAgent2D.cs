using UnityEngine;


namespace Hellscape.Gameplay {
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyAgent2D : MonoBehaviour {
        public Transform target; public float speed = 2.5f;
        private Rigidbody2D rb;
        void Awake(){ 
            rb = GetComponent<Rigidbody2D>(); 
        }
        void FixedUpdate(){ 
            if (target==null){
                return; 
            }
            var dir = (target.position - transform.position).normalized; rb.linearVelocity = dir * speed; 
        }
    }
}