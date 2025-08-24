using System.Collections.Generic;
using Hellscape.Domain;
using Vector2 = UnityEngine.Vector2;
namespace Hellscape.Net
{
    // Implemented by the App layer (SimGameServer).
    public interface INetSimBridge
    {
        void RegisterNetPlayerServer(NetPlayer player);
        void SubmitInputFrom(ulong clientId, Vector2 move, Vector2 aim, byte buttons = 0);
        
                       // Inventory methods
               int RegisterPlayerWithInventory(Vector2 spawn);
               InventoryState GetInventory(int actorId);
               InventoryState SetActiveSlot(int actorId, int index);
               InventoryState ApplyPickup(int actorId, PickupData loot, out bool dropped, out PickupData droppedPickup);
               ConsumeResult TryConsumeAmmo(int actorId);
               
               // Weapon spawning methods
               List<WeaponSpawnRequest> GetPendingWeaponSpawns();
               void SpawnWeaponPickup(WeaponSpawnRequest spawnRequest);
    }

    // Static hook where the App layer installs its bridge instance.
    public static class NetSim
    {
        public static INetSimBridge Bridge;
    }
}