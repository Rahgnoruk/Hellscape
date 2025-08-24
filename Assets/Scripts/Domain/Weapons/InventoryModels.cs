using UnityEngine;

namespace Hellscape.Domain {
  public struct WeaponSlot {
    public WeaponType type;
    public int ammo; // -1 == infinite
    public WeaponSlot(WeaponType t, int a){ type=t; ammo=a; }
    public static WeaponSlot Empty => new WeaponSlot(WeaponType.None, 0);
    public bool IsEmpty => type == WeaponType.None;
    public bool IsInfinite => ammo < 0;
  }

  public struct InventoryState {
    public WeaponSlot s0, s1, s2, s3;
    public int ActiveIndex; // 0..3

    public WeaponSlot Get(int i) => i==0? s0 : (i==1? s1 : (i==2? s2 : s3));
    public void Set(int i, WeaponSlot v){ if(i==0) s0=v; else if(i==1) s1=v; else if(i==2) s2=v; else s3=v; }

    public static InventoryState NewWithBase() {
      var inv = new InventoryState {
        s0 = new WeaponSlot(WeaponType.BasePistol, -1),
        s1 = WeaponSlot.Empty,
        s2 = WeaponSlot.Empty,
        s3 = WeaponSlot.Empty,
        ActiveIndex = 0
      };
      return inv;
    }
  }

  public struct PickupData {
    public WeaponType type;
    public int ammo;
    public PickupData(WeaponType t, int a){ type=t; ammo=a; }
  }

  public struct ConsumeResult {
    public bool fired;
    public InventoryState next;
  }

  public interface ISimApi {
    // Port exposed to outer layers (Net/Presentation) — implemented by App (wrapping ServerSim).
    int RegisterPlayer(Vector2 spawn);
    InventoryState GetInventory(int actorId);
    InventoryState SetActiveSlot(int actorId, int index);
    InventoryState ApplyPickup(int actorId, PickupData loot, out bool dropped, out PickupData droppedPickup);
    ConsumeResult TryConsumeAmmo(int actorId); // consumes 1 shot from active slot if not infinite
  }
}
