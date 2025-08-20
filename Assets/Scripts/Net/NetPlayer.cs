using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Hellscape.Net
{
    public sealed class NetPlayer : NetworkBehaviour, HellscapeControls.IPlayerActions
    {
        [SerializeField] float rpcRate = 20f; // Hz

        public readonly NetworkVariable<Vector2> netPos =
          new(writePerm: NetworkVariableWritePermission.Server);

        private HellscapeControls _controls;
        private Vector2 _moveInput;
        private float _rpcAccum;

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

        void Update()
        {
            // Everyone renders from replicated position
            transform.position = netPos.Value;

            if (!IsOwner || _controls == null) return;

            _rpcAccum += Time.deltaTime;
            if (_rpcAccum >= (1f / Mathf.Max(1f, rpcRate)))
            {
                var mv = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
                SubmitInputServerRpc(mv);
                _rpcAccum = 0f;
            }
        }

        [ServerRpc(RequireOwnership = true)]
        void SubmitInputServerRpc(Vector2 move)
        {
            NetSim.Bridge?.SubmitInputFrom(OwnerClientId, move);
        }

        // Input callbacks (owner)
        public void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
        public void OnLook(InputAction.CallbackContext ctx) { }
        public void OnAttack(InputAction.CallbackContext ctx) { }
        public void OnInteract(InputAction.CallbackContext ctx) { }
        public void OnCrouch(InputAction.CallbackContext ctx) { }
        public void OnJump(InputAction.CallbackContext ctx) { }
        public void OnPrevious(InputAction.CallbackContext ctx) { }
        public void OnNext(InputAction.CallbackContext ctx) { }
        public void OnDash(InputAction.CallbackContext ctx) { }
    }
}
