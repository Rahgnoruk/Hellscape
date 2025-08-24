using Hellscape.Domain;
using UnityEngine;
using Unity.Netcode;

namespace Hellscape.Net
{
    public class WeaponPickup : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.1f;
        
        public readonly NetworkVariable<byte> weaponType = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<int> ammo = new(writePerm: NetworkVariableWritePermission.Server);
        
        private Vector3 _originalPosition;
        private float _bobTime;

        public override void OnNetworkSpawn()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            _originalPosition = transform.position;
            
            // Set layer to Pickup
            gameObject.layer = LayerMask.NameToLayer("Pickup");
        }

        private void Update()
        {
            // Client-side idle bob animation
            if (!IsServer)
            {
                _bobTime += Time.deltaTime * bobSpeed;
                var bobOffset = Mathf.Sin(_bobTime) * bobHeight;
                transform.position = _originalPosition + Vector3.up * bobOffset;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void InitServerRpc(byte weaponType, int ammo)
        {
            this.weaponType.Value = weaponType;
            this.ammo.Value = ammo;
        }

        public void InitServer(WeaponType type, int ammo)
        {
            weaponType.Value = (byte)type;
            this.ammo.Value = ammo;
        }
    }
}
