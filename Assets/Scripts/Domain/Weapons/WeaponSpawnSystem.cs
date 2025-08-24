using System;
using System.Collections.Generic;

namespace Hellscape.Domain
{
    public struct WeaponSpawnRequest
    {
        public WeaponType type;
        public int ammo;
        public Vector2 position;
        public float spawnTime;
        
        public WeaponSpawnRequest(WeaponType type, int ammo, Vector2 position, float spawnTime)
        {
            this.type = type;
            this.ammo = ammo;
            this.position = position;
            this.spawnTime = spawnTime;
        }
    }
    
    public class WeaponSpawnSystem
    {
        private readonly DeterministicRng _rng;
        private readonly Vector2 _playfieldHalfExtents;
        private float _nextSpawnTime;
        private readonly float _spawnInterval;
        private readonly int _minAmmo;
        private readonly int _maxAmmo;
        
        private readonly WeaponType[] _spawnableWeapons = { 
            WeaponType.Rifle, 
            WeaponType.SMG, 
            WeaponType.Shotgun 
        };
        
        public WeaponSpawnSystem(DeterministicRng rng, Vector2 playfieldHalfExtents, 
            float spawnInterval = 10f, int minAmmo = 10, int maxAmmo = 30)
        {
            _rng = rng;
            _playfieldHalfExtents = playfieldHalfExtents;
            _spawnInterval = spawnInterval;
            _minAmmo = minAmmo;
            _maxAmmo = maxAmmo;
            _nextSpawnTime = spawnInterval; // First spawn after interval
        }
        
        public List<WeaponSpawnRequest> Update(float currentTime, float deltaTime)
        {
            var spawns = new List<WeaponSpawnRequest>();
            
            if (currentTime >= _nextSpawnTime)
            {
                // Generate spawn request
                var weaponType = _spawnableWeapons[_rng.Range(0, _spawnableWeapons.Length)];
                var ammo = _rng.Range(_minAmmo, _maxAmmo + 1);
                var position = GetRandomSpawnPosition();
                
                spawns.Add(new WeaponSpawnRequest(weaponType, ammo, position, currentTime));
                
                // Schedule next spawn
                _nextSpawnTime = currentTime + _spawnInterval;
            }
            
            return spawns;
        }
        
        private Vector2 GetRandomSpawnPosition()
        {
            // Spawn weapons in the outer areas of the playfield, avoiding the center
            var centerRadius = 8f; // Avoid center area
            
            Vector2 position;
            do
            {
                position = new Vector2(
                    _rng.RangeFloat(-_playfieldHalfExtents.x, _playfieldHalfExtents.x),
                    _rng.RangeFloat(-_playfieldHalfExtents.y, _playfieldHalfExtents.y)
                );
            } while (Vector2Magnitude(position) < centerRadius);
            
            return position;
        }
        
        private static float Vector2Magnitude(Vector2 v)
        {
            return (float)System.Math.Sqrt(v.x * v.x + v.y * v.y);
        }
    }
}
