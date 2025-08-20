using UnityEngine;
using UnityEngine.InputSystem;
using Hellscape.Domain;
using Hellscape.Net;
using Vector2 = UnityEngine.Vector2;

namespace Hellscape.App {
    public class InputAdapter : MonoBehaviour, HellscapeControls.IPlayerActions {
        private LocalTransport transport;
        private ServerSim serverSim;
        private HellscapeControls controls;
        
        private int currentTick;
        private Vector2 moveInput;
        private Vector2 lookInput;
        private bool dashPressed;
        private bool attackPressed;
        
        public void Initialize(LocalTransport transport, ServerSim serverSim) {
            this.transport = transport;
            this.serverSim = serverSim;
            
            // Initialize Input System
            controls = new HellscapeControls();
            controls.Player.SetCallbacks(this);
            controls.Player.Enable();
            
            // Wire up the transport to send input commands to the sim
            transport.OnServerMsg += HandleInputMessage;
        }
        
        private void Update() {
            // Create InputCommand from current input state
            byte buttons = 0;
            if (dashPressed) {
                buttons |= MovementConstants.DashButtonBit;
            }
            if (attackPressed) {
                buttons |= MovementConstants.AttackButtonBit;
            }
            
            var command = new InputCommand(currentTick, moveInput.x, moveInput.y, lookInput.x, lookInput.y, buttons);
            
            // Apply command directly to simulation
            if (serverSim != null) {
                serverSim.Apply(command);
            }
            
            currentTick++;
        }
        
        private void OnDestroy() {
            if (controls != null) {
                controls.Player.Disable();
                controls.Dispose();
            }
        }
        
        private void HandleInputMessage(byte[] data) {
            // For now, just apply the command directly to the sim
            // In a real implementation, this would decode the message
            if (serverSim != null) {
                // Create a simple command from current input state
                byte buttons = 0;
                if (dashPressed) {
                    buttons |= MovementConstants.DashButtonBit;
                }
                if (attackPressed) {
                    buttons |= MovementConstants.AttackButtonBit;
                }
                
                var command = new InputCommand(currentTick, moveInput.x, moveInput.y, lookInput.x, lookInput.y, buttons);
                serverSim.Apply(command);
            }
        }
        
        // Input System callbacks
        public void OnMove(InputAction.CallbackContext context) {
            moveInput = context.ReadValue<Vector2>();
        }
        
        public void OnLook(InputAction.CallbackContext context) {
            lookInput = context.ReadValue<Vector2>();
        }
        
        public void OnAttack(InputAction.CallbackContext context) {
            attackPressed = context.performed;
        }
        
        public void OnInteract(InputAction.CallbackContext context) {
            // Not used for movement system
        }
        
        public void OnCrouch(InputAction.CallbackContext context) {
            // Not used for movement system
        }
        
        public void OnJump(InputAction.CallbackContext context) {
            // Not used for movement system
        }
        
        public void OnPrevious(InputAction.CallbackContext context) {
            // Not used for movement system
        }
        
        public void OnNext(InputAction.CallbackContext context) {
            // Not used for movement system
        }
        
        public void OnDash(InputAction.CallbackContext context) {
            dashPressed = context.performed;
        }
    }
}
