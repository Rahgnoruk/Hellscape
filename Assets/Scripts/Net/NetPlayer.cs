using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Hellscape.Domain;
using Vector2 = UnityEngine.Vector2;

namespace Hellscape.Net
{
    public sealed class NetPlayer : NetworkBehaviour, HellscapeControls.IPlayerActions
    {
        [SerializeField] float rpcRate = 20f; // Hz

        public readonly NetworkVariable<Vector2> netPos =
          new(writePerm: NetworkVariableWritePermission.Server);

        private HellscapeControls _controls;
        private Vector2 _moveInput;
        private Vector2 _aimPosInput;
        private Vector2 _aimStickInput;
        private float _rpcAccum;
        private bool _fireHeld;
        private bool _firePressedThisTick;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                _controls = new HellscapeControls();
                _controls.Player.SetCallbacks(this);
                _controls.Player.Enable();
            }
            if (IsServer)
            {
                // Link this view to a domain actor in the sim
                if (NetSim.Bridge == null)
                {
                    Debug.LogError("NetSim.Bridge is not set! Ensure SimGameServer is running.");
                    return;
                }
                NetSim.Bridge?.RegisterNetPlayerServer(this);
                netPos.Value = transform.position;
            }

            
        }

        public override void OnNetworkDespawn()
        {
            if (_controls != null) { _controls.Player.Disable(); _controls.Dispose(); _controls = null; }
        }

        Vector2 ComputeAim()
        {
            // Prefer AimPos (mouse/touch position) if available
            if (_aimPosInput.sqrMagnitude > 0.0001f) {
                var world = Camera.main != null
                    ? (Vector2)Camera.main.ScreenToWorldPoint(new Vector3(_aimPosInput.x, _aimPosInput.y, 0f))
                    : (Vector2)transform.position;
                var dir = world - (Vector2)transform.position;
                if (dir.sqrMagnitude > 0.0001f) return dir.normalized;
            }
            
            // Fallback to AimStick (gamepad right stick)
            if (_aimStickInput.sqrMagnitude > 0.16f) { // deadzone
                return _aimStickInput.normalized;
            }
            
            return Vector2.right; // default direction
        }
        
        byte BuildButtons()
        {
            byte b = 0;
            if (_fireHeld || _firePressedThisTick)
            {
                b |= MovementConstants.AttackButtonBit;
            }
            // Fire
            // if you wire dash later: if (_dashPressedThisTick) b |= MovementConstants.DashButtonBit;
            _firePressedThisTick = false; // edge reset each tick window
            return b;
        }
        
        void Update()
        {
            transform.position = netPos.Value;
            if (_controls == null) return;
            _rpcAccum += Time.deltaTime;
            if (_rpcAccum >= (1f / Mathf.Max(1f, rpcRate)))
            {
                var mv = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
                var aim = ComputeAim();
                var buttons = BuildButtons();
                SubmitInputServerRpc(mv, aim, buttons);
                _rpcAccum = 0f;
            }
            if (!IsOwner)
            {
                transform.position = Vector2.Lerp(transform.position, netPos.Value, 0.18f);
            }
            else
            {
                transform.position = netPos.Value;
            }
        }

        [ServerRpc(RequireOwnership = true)]
        void SubmitInputServerRpc(Vector2 move, Vector2 aim, byte buttons)
        {
            NetSim.Bridge?.SubmitInputFrom(OwnerClientId, move, aim, buttons);
        }

        // Input callbacks (owner)
        public void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
        public void OnLook(InputAction.CallbackContext ctx) { 
            _aimPosInput = ctx.ReadValue<Vector2>();
        }
        public void OnAttack(InputAction.CallbackContext ctx) {
            if (ctx.performed)
            {
                _fireHeld = true;
                _firePressedThisTick = true;
            }
            else if (ctx.canceled)
            {
                _fireHeld = false;
            }
        }
        public void OnInteract(InputAction.CallbackContext ctx) { }
        public void OnCrouch(InputAction.CallbackContext ctx) { }
        public void OnJump(InputAction.CallbackContext ctx) { }
        public void OnPrevious(InputAction.CallbackContext ctx) { }
        public void OnNext(InputAction.CallbackContext ctx) { }
        public void OnDash(InputAction.CallbackContext ctx) { }
    }
}
