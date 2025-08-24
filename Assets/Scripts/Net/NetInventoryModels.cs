using Hellscape.Domain;
using Unity.Netcode;
using System;

namespace Hellscape.Net
{
    public struct NetWeaponSlot : INetworkSerializable, IEquatable<NetWeaponSlot>
    {
        public byte type;
        public int ammo;
        
        public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter
        {
            s.SerializeValue(ref type);
            s.SerializeValue(ref ammo);
        }
        
        public static NetWeaponSlot From(WeaponSlot s) => new() { type = (byte)s.type, ammo = s.ammo };
        
        public WeaponSlot ToDomain() => new WeaponSlot((WeaponType)type, ammo);
        
        public bool Equals(NetWeaponSlot other)
        {
            return type == other.type && ammo == other.ammo;
        }
        
        public override bool Equals(object obj)
        {
            return obj is NetWeaponSlot other && Equals(other);
        }
        
        public override int GetHashCode()
        {
            return HashCode.Combine(type, ammo);
        }
        
        public static bool operator ==(NetWeaponSlot left, NetWeaponSlot right)
        {
            return left.Equals(right);
        }
        
        public static bool operator !=(NetWeaponSlot left, NetWeaponSlot right)
        {
            return !left.Equals(right);
        }
    }
}
