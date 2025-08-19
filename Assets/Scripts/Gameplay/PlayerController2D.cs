using UnityEngine;


namespace Hellscape.Gameplay {
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController2D : MonoBehaviour {
        public float moveSpeed = 6f;
        private Rigidbody2D rb;
        private Vector2 input;


        void Awake(){ 
            rb = GetComponent<Rigidbody2D>(); 
        }
        void Update(){
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        }
        void FixedUpdate(){ 
            rb.linearVelocity = input * moveSpeed; 
        }
    }
}