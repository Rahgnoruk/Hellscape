using UnityEngine;
using UnityEngine.InputSystem;
using Hellscape.Domain;
using Vector2 = UnityEngine.Vector2;

namespace Hellscape.App {
    public class InputAdapter : MonoBehaviour, HellscapeControls.IPlayerActions {
        private ServerSim serverSim;
        private int playerId;
        private HellscapeControls controls;
        
        private int currentTick;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool dashPressedThisTick;   // latched
        private bool attackHeld;
        private bool attackPressedThisTick; // edge

        public void Initialize(ServerSim serverSim, int playerId) {
            this.serverSim = serverSim;
            this.playerId = playerId;

            // Initialize Input System
            controls = new HellscapeControls();
            controls.Player.SetCallbacks(this);
            controls.Player.Enable();
        }
        private void OnDestroy()
        {
            if (controls != null)
            {
                controls.Player.Disable();
                controls.Dispose();
                controls = null;
            }
        }

        private void FixedUpdate() {
            if (serverSim == null) return;

            // Create InputCommand from current input state
            byte buttons = 0;
            if (dashPressedThisTick) {
                buttons |= MovementConstants.DashButtonBit;
            }
            if (attackPressedThisTick || attackHeld) {
                buttons |= MovementConstants.AttackButtonBit;
            }

            // Normalize move to avoid faster diagonals
            var move = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;

            var command = new InputCommand(currentTick, move.x, move.y, lookInput.x, lookInput.y, buttons);
            
            // Apply command directly to simulation
            if (serverSim != null) {
                serverSim.ApplyForActor(playerId, command);
            }
            
            currentTick++;
            dashPressedThisTick = false; // Reset for next tick
            attackPressedThisTick = false; // Reset for next tick
        }
        
        // Input System callbacks
        public void OnMove(InputAction.CallbackContext context) {
            moveInput = context.ReadValue<Vector2>();
        }
        
        public void OnLook(InputAction.CallbackContext context) {
            lookInput = context.ReadValue<Vector2>();
        }
        
        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.performed) { 
                attackHeld = true; 
                attackPressedThisTick = true; 
            }
            if (context.canceled) {
                attackHeld = false;
            }
        }
        
        public void OnInteract(InputAction.CallbackContext context) { }
        public void OnCrouch(InputAction.CallbackContext ctx) { }
        public void OnJump(InputAction.CallbackContext ctx) { }
        public void OnPrevious(InputAction.CallbackContext ctx) { }
        public void OnNext(InputAction.CallbackContext ctx) { }
        public void OnDash(InputAction.CallbackContext context) {
            dashPressedThisTick = context.performed;
        }
        public void OnWeaponSlot0(InputAction.CallbackContext ctx) { }
        public void OnWeaponSlot1(InputAction.CallbackContext ctx) { }
        public void OnWeaponSlot2(InputAction.CallbackContext ctx) { }
        public void OnWeaponSlot3(InputAction.CallbackContext ctx) { }
    }
}
