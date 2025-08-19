using UnityEngine;
using UnityEngine.InputSystem;


namespace Hellscape.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerInputHandler : MonoBehaviour
    {
        public Camera worldCamera; // assign main camera


        private Rigidbody2D rb;
        private HellscapeControls controls;


        private Vector2 move;
        private Vector2 lookStick; // gamepad right stick
        private bool fireHeld;
        private bool dashPressed;
        private bool usePressed;


        public float moveSpeed = 6f;


        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            controls = new HellscapeControls();


            // Movement
            controls.Player.Move.performed += ctx => move = ctx.ReadValue<Vector2>();
            controls.Player.Move.canceled += _ => move = Vector2.zero;


            // Gamepad look
            controls.Player.Look.performed += ctx => lookStick = ctx.ReadValue<Vector2>();
            controls.Player.Look.canceled += _ => lookStick = Vector2.zero;


            // Buttons
            controls.Player.Attack.performed += _ => fireHeld = true;
            controls.Player.Attack.canceled += _ => fireHeld = false;
            controls.Player.Dash.performed += _ => dashPressed = true; // read & consume in FixedUpdate
            controls.Player.Interact.performed += _ => usePressed = true; // read & consume in FixedUpdate
        }


        void OnEnable() { controls.Enable(); }
        void OnDisable() { controls.Disable(); }


        void Update()
        {
            // Aim resolution: mouse wins when moved; otherwise right stick.
            Vector2 aim;
            if (Mouse.current != null && worldCamera)
            {
                var mousePos = Mouse.current.position.ReadValue();
                var world = (Vector2)worldCamera.ScreenToWorldPoint(mousePos);
                aim = (world - (Vector2)transform.position);
            }
            else
            {
                aim = lookStick; // right stick vector, already in [-1,1]
            }
            if (aim.sqrMagnitude > 0.001f)
            {
                // Example: rotate a child (gun/hands) toward aim
                float z = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, z);
            }
        }


        void FixedUpdate()
        {
            rb.linearVelocity = move.normalized * moveSpeed;


            if (dashPressed)
            {
                // TODO: dash impulse
                dashPressed = false; // consume
            }
            if (usePressed)
            {
                // TODO: interact
                usePressed = false; // consume
            }


            if (fireHeld)
            {
                // TODO: fire primary
            }
        }
    }
}