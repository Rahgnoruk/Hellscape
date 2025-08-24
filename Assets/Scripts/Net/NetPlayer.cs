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
        [SerializeField] SpriteRenderer spriteRenderer; // assign in inspector
        [SerializeField] Transform muzzleTransform; // assign in inspector - child of weapon view

        public readonly NetworkVariable<Vector2> netPos =
          new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<short> netHp = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<bool> netAlive = new(writePerm: NetworkVariableWritePermission.Server);
        
        // Inventory replication
        public readonly NetworkList<NetWeaponSlot> netSlots = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<int> activeIndex = new(writePerm: NetworkVariableWritePermission.Server);

        private HellscapeControls _controls;
        private Vector2 _moveInput;
        private Vector2 _aimPosInput;
        private Vector2 _aimStickInput;
        private float _rpcAccum;
        private bool _fireHeld;
        private bool _firePressedThisTick;
        
        // Inventory state
        private int _actorId = -1;

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
                
                // Register player with inventory
                _actorId = NetSim.Bridge.RegisterPlayerWithInventory(transform.position);
                
                // Initialize inventory
                var inventory = NetSim.Bridge.GetInventory(_actorId);
                MirrorInventoryToNetwork(inventory);
                
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
        
        Vector2 GetMuzzleWorldPosition()
        {
            if (muzzleTransform != null)
            {
                return muzzleTransform.position;
            }
            // Fallback to player position + forward
            return transform.position + (Vector3)ComputeAim();
        }
        
        void Update()
        {
            transform.position = netPos.Value;
            
            // Visual feedback for dead players
            if (spriteRenderer != null)
            {
                if (!netAlive.Value)
                {
                    // Gray out dead players
                    spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
                else
                {
                    // Normal color for alive players
                    spriteRenderer.color = Color.white;
                }
            }
            
            if (_controls == null) return;
            
            // Ignore input if dead
            if (IsOwner && netAlive.Value == false) return;
            
            _rpcAccum += Time.deltaTime;
            if (_rpcAccum >= (1f / Mathf.Max(1f, rpcRate)))
            {
                var mv = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
                var aim = ComputeAim();
                var buttons = BuildButtons();
                SubmitInputServerRpc(mv, aim, buttons);
                
                // Handle firing with muzzle-based system
                if (_fireHeld || _firePressedThisTick)
                {
                    var muzzlePos = GetMuzzleWorldPosition();
                    var aimDir = ComputeAim();
                    FireServerRpc(muzzlePos, aimDir);
                }
                
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
            // TODO: Update to use ISimApi for input submission
            // For now, keep using NetSim.Bridge for backward compatibility
            NetSim.Bridge?.SubmitInputFrom(OwnerClientId, move, aim, buttons);
        }

        [ServerRpc(RequireOwnership = true)]
        void RequestPickupNearestServerRpc()
        {
            if (NetSim.Bridge == null || _actorId == -1) return;
            
            // Find nearest pickup using Physics2D
            var colliders = new Collider2D[10];
            var count = Physics2D.OverlapCircleNonAlloc(transform.position, 2f, colliders, LayerMask.GetMask("Pickup"));
            
            if (count > 0)
            {
                // Find nearest pickup
                var nearest = colliders[0];
                var nearestDist = Vector2.Distance(transform.position, nearest.transform.position);
                
                for (int i = 1; i < count; i++)
                {
                    var dist = Vector2.Distance(transform.position, colliders[i].transform.position);
                    if (dist < nearestDist)
                    {
                        nearest = colliders[i];
                        nearestDist = dist;
                    }
                }
                
                var pickup = nearest.GetComponent<WeaponPickup>();
                if (pickup != null)
                {
                    var loot = new PickupData((WeaponType)pickup.weaponType.Value, pickup.ammo.Value);
                    var newInventory = NetSim.Bridge.ApplyPickup(_actorId, loot, out bool dropped, out var droppedPickup);
                    
                    // Mirror inventory to network
                    MirrorInventoryToNetwork(newInventory);
                    
                    // Handle dropped weapon
                    if (dropped)
                    {
                        SpawnDroppedWeaponServerRpc((byte)droppedPickup.type, droppedPickup.ammo, transform.position);
                    }
                    
                    // Despawn consumed pickup
                    pickup.NetworkObject.Despawn();
                }
            }
        }

        [ServerRpc(RequireOwnership = true)]
        void SetActiveSlotServerRpc(int index)
        {
            if (NetSim.Bridge == null || _actorId == -1) return;
            
            var newInventory = NetSim.Bridge.SetActiveSlot(_actorId, index);
            MirrorInventoryToNetwork(newInventory);
        }

        [ServerRpc(RequireOwnership = true)]
        void FireServerRpc(Vector2 muzzleWorld, Vector2 dir)
        {
            if (NetSim.Bridge == null || _actorId == -1) return;
            
            // Try to consume ammo
            var consumeResult = NetSim.Bridge.TryConsumeAmmo(_actorId);
            if (!consumeResult.fired) return;
            
            // Update inventory if ammo was consumed
            MirrorInventoryToNetwork(consumeResult.next);
            
            // TODO: Perform hit logic using muzzleWorld + dir
            // For now, just trigger VFX
            PlayShotVfxClientRpc(muzzleWorld, muzzleWorld + dir * 10f);
        }

        [ServerRpc(RequireOwnership = false)]
        void SpawnDroppedWeaponServerRpc(byte weaponType, int ammo, Vector2 position)
        {
            // Use the bridge to spawn dropped weapon
            // The App layer (SimGameServer) will handle the actual spawning
            if (NetSim.Bridge != null)
            {
                var spawnRequest = new WeaponSpawnRequest((WeaponType)weaponType, ammo, new Domain.Vector2(position.x, position.y), 0f);
                NetSim.Bridge.SpawnWeaponPickup(spawnRequest);
            }
        }

        [ClientRpc]
        void PlayShotVfxClientRpc(Vector2 from, Vector2 to)
        {
            // Broadcast VFX event through domain system
            Domain.Vector2 domainFrom = new Domain.Vector2(from.x, from.y);
            Domain.Vector2 domainTo = new Domain.Vector2(to.x, to.y);
            VfxEventSystem.Broadcast(VfxEvent.Shot(domainFrom, domainTo));
        }

        private void MirrorInventoryToNetwork(InventoryState inventory)
        {
            // Clear and repopulate network list
            netSlots.Clear();
            netSlots.Add(NetWeaponSlot.From(inventory.s0));
            netSlots.Add(NetWeaponSlot.From(inventory.s1));
            netSlots.Add(NetWeaponSlot.From(inventory.s2));
            netSlots.Add(NetWeaponSlot.From(inventory.s3));
            
            activeIndex.Value = inventory.ActiveIndex;
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
        public void OnInteract(InputAction.CallbackContext ctx) 
        { 
            if (ctx.performed && IsOwner)
            {
                RequestPickupNearestServerRpc();
            }
        }
        public void OnCrouch(InputAction.CallbackContext ctx) { }
        public void OnJump(InputAction.CallbackContext ctx) { }
        public void OnPrevious(InputAction.CallbackContext ctx) { }
        public void OnNext(InputAction.CallbackContext ctx) { }
        public void OnDash(InputAction.CallbackContext ctx) { }
        public void OnWeaponSlot0(InputAction.CallbackContext ctx) 
        { 
            if (ctx.performed && IsOwner)
            {
                SetActiveSlotServerRpc(0);
            }
        }
        public void OnWeaponSlot1(InputAction.CallbackContext ctx) 
        { 
            if (ctx.performed && IsOwner)
            {
                SetActiveSlotServerRpc(1);
            }
        }
        public void OnWeaponSlot2(InputAction.CallbackContext ctx) 
        { 
            if (ctx.performed && IsOwner)
            {
                SetActiveSlotServerRpc(2);
            }
        }
        public void OnWeaponSlot3(InputAction.CallbackContext ctx) 
        { 
            if (ctx.performed && IsOwner)
            {
                SetActiveSlotServerRpc(3);
            }
        }
    }
}
