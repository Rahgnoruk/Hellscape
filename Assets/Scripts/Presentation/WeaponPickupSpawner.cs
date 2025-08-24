using Hellscape.Domain;
using Hellscape.Net;
using UnityEngine;
using Unity.Netcode;
namespace Hellscape.Presentation
{
    public class WeaponPickupSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject weaponPickupPrefab;
        [SerializeField] private BoxCollider2D spawnArea;
        [SerializeField] private float spawnInterval = 10f;
        [SerializeField] private int minAmmo = 10;
        [SerializeField] private int maxAmmo = 30;
        
        private float _spawnTimer;
        private readonly WeaponType[] _weaponTypes = { WeaponType.Rifle, WeaponType.SMG, WeaponType.Shotgun };

        private void Update()
        {
            if (!NetworkManager.Singleton.IsServer) return;
            
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= spawnInterval)
            {
                SpawnRandomPickup();
                _spawnTimer = 0f;
            }
        }

        private void SpawnRandomPickup()
        {
            if (weaponPickupPrefab == null || spawnArea == null) return;
            
            // Get random position within spawn area
            var randomPoint = GetRandomPointInArea();
            
            // Spawn the pickup
            var pickupObj = Instantiate(weaponPickupPrefab, randomPoint, Quaternion.identity);
            var pickup = pickupObj.GetComponent<WeaponPickup>();
            
            if (pickup != null)
            {
                // Random weapon type and ammo
                var weaponType = _weaponTypes[Random.Range(0, _weaponTypes.Length)];
                var ammo = Random.Range(minAmmo, maxAmmo + 1);
                
                pickup.InitServer(weaponType, ammo);
                
                // Spawn on network
                pickupObj.GetComponent<NetworkObject>().Spawn();
            }
        }

        private Vector3 GetRandomPointInArea()
        {
            var bounds = spawnArea.bounds;
            var randomX = Random.Range(bounds.min.x, bounds.max.x);
            var randomY = Random.Range(bounds.min.y, bounds.max.y);
            return new Vector3(randomX, randomY, 0f);
        }
    }
}
