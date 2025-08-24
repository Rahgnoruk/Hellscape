You are Cursor working on the Hellscape repo with .cursorrules loaded
Do NOT run dotnet test. For tests, use Unity’s CLI:
Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1


Goal (this round)
Authoritative weapon pickups + inventory + weapon switching, with ammo tracking and muzzle-based shot VFX (muzzle flash, tracer, impact spark) for the active weapon. No new gameplay effects (e.g., shotgun pushback) this round—just visuals.
Players:
Start with a permanent BasePistol (infinite ammo) in slot 0.


Have three pickup slots (1–3).


Can switch active slot via input (slot0…slot3).


Can pick up weapons placed around the map: merge ammo if same type; otherwise place in empty slot, or swap by dropping the replaced weapon on the ground (never drop slot0).


Shots originate at the current weapon’s muzzle transform (child of player’s weapon view). VFX must use this exact world position + aim direction.


Networking:
Server authoritative for pickups, inventory changes, ammo consumption, and fire requests.


Replicate inventory to clients for HUD.


UI:
Update the existing HUD to show slots, active highlight, weapon names, and ammo (∞ for base).


Determinism/Architecture:
Domain owns inventory rules and DTOs. No UnityEngine in Domain.


Net/Presentation implement Unity/NGO details.


App composes everything and registers ports. No circular deps: Domain ← Net/Presentation; App references all, nothing references App.



Architecture contracts (add/adjust)
1) Domain (pure C#; no Unity)
Files
Assets/Hellscape/Scripts/Domain/Weapons/WeaponType.cs (new)


Assets/Hellscape/Scripts/Domain/Weapons/InventoryModels.cs (new)


Assets/Hellscape/Scripts/Domain/Weapons/InventoryLogic.cs (new)


WeaponType.cs
namespace Hellscape.Domain {
  public enum WeaponType : byte {
    None = 0,
    BasePistol = 1,  // slot0 only, infinite ammo
    Rifle = 10,
    SMG   = 11,
    Shotgun = 12
  }
}
InventoryModels.cs
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
InventoryLogic.cs
namespace Hellscape.Domain {
  public static class InventoryLogic {
    public static InventoryState ApplyPickup(in InventoryState inv, in PickupData loot,
      out bool dropped, out PickupData droppedPickup)
    {
      dropped = false; droppedPickup = default;
      if (loot.type == WeaponType.None || loot.ammo <= 0) return inv;

      // Merge if same-type exists in slots 1..3
      for (int i=1;i<=3;i++){
        var s = inv.Get(i);
        if (s.type == loot.type){
          if (!s.IsInfinite) s.ammo += loot.ammo;
          var outInv = inv; outInv.Set(i, s);
          return outInv;
        }
      }
      // Place in free slot
      for (int i=1;i<=3;i++){
        if (inv.Get(i).IsEmpty){
          var outInv = inv; outInv.Set(i, new WeaponSlot(loot.type, loot.ammo));
          return outInv;
        }
      }
      // Full: replace target (active unless base; if base active, use slot1)
      int target = (inv.ActiveIndex == 0) ? 1 : inv.ActiveIndex;
      var toDrop = inv.Get(target);
      dropped = !toDrop.IsEmpty;
      if (dropped) droppedPickup = new PickupData(toDrop.type, toDrop.IsInfinite ? loot.ammo : toDrop.ammo);
      var outInv2 = inv; outInv2.Set(target, new WeaponSlot(loot.type, loot.ammo));
      return outInv2;
    }

    public static InventoryState SetActive(InventoryState inv, int index){
      if (index < 0) index = 0; if (index > 3) index = 3;
      inv.ActiveIndex = index;
      return inv;
    }

    public static ConsumeResult TryConsume(InventoryState inv){
      var s = inv.Get(inv.ActiveIndex);
      if (s.IsEmpty) return new ConsumeResult{ fired=false, next=inv };
      if (s.IsInfinite) return new ConsumeResult{ fired=true, next=inv };
      if (s.ammo <= 0) return new ConsumeResult{ fired=false, next=inv };
      s.ammo -= 1;
      inv.Set(inv.ActiveIndex, s);
      return new ConsumeResult{ fired=true, next=inv };
    }
  }
}


Important: The ISimApi interface lives in Domain (port). It will be implemented in App (wrapping ServerSim and holding per-player inventories). Net will only depend on ISimApi, avoiding Net↔App cycles.
2) Tests (EditMode)
Assets/Hellscape/Tests/EditMode/InventoryLogicTests.cs (new)
 Covers: NewWithBase, merge same type, free slot, full+active0 replaces slot1, full+active2 replaces 2, base never dropped, SetActive, TryConsume on finite/infinite/empty.
Run with:
- Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1
Composition root (App)
Create a tiny service locator and the ISimApi adapter.
Files
Assets/Hellscape/Scripts/App/Services/ServiceLocator.cs (new)


Assets/Hellscape/Scripts/App/Sim/SimApiAdapter.cs (new) // implements Domain.ISimApi, wraps ServerSim + per-player inventories


Assets/Hellscape/Scripts/App/Installers/GameInstaller.cs (new) // registers ports at boot


Rules
App references Domain + Net + Presentation.


Net/Presentation reference Domain only (plus Unity/NGO). They Resolve<ISimApi>() at runtime. No assembly cycles.


ServiceLocator.cs (simple generic Register/Resolve).
SimApiAdapter.cs
Holds ServerSim instance and a Dictionary<int, InventoryState>.


Implements all ISimApi methods using InventoryLogic + calls into ServerSim as needed (for positions, actors, etc.).


For now, ServerSim remains the authority for movement/combat; SimApiAdapter keeps inventory state authoritative and serializes it to Net for HUD.


GameInstaller.cs
On scene boot (before networking), create/keep a root GO:


Construct ServerSim (seeded), start it.


Construct SimApiAdapter wrapping that sim.


ServiceLocator.Register<ISimApi>(simApiAdapter);


This removes the previous NetSim.Bridge coupling. Delete that and replace with ISimApi.

Net & Presentation
Net models & replication
Files
Assets/Hellscape/Scripts/Net/NetInventoryModels.cs (new)


Modify NetPlayer.cs (add inventory replication & RPCs)


Assets/Hellscape/Scripts/Net/WeaponPickup.cs (new)


Assets/Hellscape/Scripts/Presentation/WeaponPickupSpawner.cs (new)


NetInventoryModels.cs
using Hellscape.Domain;
using Unity.Netcode;

namespace Hellscape.Net {
  public struct NetWeaponSlot : INetworkSerializable {
    public byte type; public int ammo;
    public void NetworkSerialize<T>(BufferSerializer<T> s) where T : IReaderWriter {
      s.SerializeValue(ref type); s.SerializeValue(ref ammo);
    }
    public static NetWeaponSlot From(WeaponSlot s) => new(){ type=(byte)s.type, ammo=s.ammo };
  }
}
WeaponPickup (NetworkBehaviour)
Server-owned, layer “Pickup”.


NetworkVariable<byte> weaponType, NetworkVariable<int> ammo.


InitServer(WeaponType t, int ammo).


Minimal sprite; add idle bob in Update() (client-side).


WeaponPickupSpawner (server-only)
Uses a BoxCollider2D area to spawn pickups over time (random types from Rifle/SMG/Shotgun with configured ammo).


Server Spawn() after InitServer.


NetPlayer changes
Add NetworkList<NetWeaponSlot> netSlots (size 4) and a NetworkVariable<int> activeIndex. Server writes; others read.


On server spawn:


actorId = ISimApi.RegisterPlayer(spawn).


var inv = ISimApi.GetInventory(actorId) (should be NewWithBase).


Mirror to netSlots + activeIndex.


Interact input (owner):


[ServerRpc] RequestPickupNearestServerRpc() → server does Physics2D.OverlapCircleNonAlloc on Pickup layer, picks nearest, builds PickupData, calls ISimApi.ApplyPickup(actorId, loot, out dropped, out droppedLoot). If dropped==true, spawn a new WeaponPickup at player pos with droppedLoot. Despawn consumed pickup. Mirror inventory to netSlots + activeIndex.


Slot select inputs (owner):


[ServerRpc] SetActiveSlotServerRpc(int index) → ISimApi.SetActiveSlot(actorId, index), mirror to network list/activeIndex. The HUD highlights accordingly.


Fire input (owner):


Compute muzzle world position from the child transform Muzzle under the active weapon view; also compute aim direction from current control scheme (mouse/touch sends world position already).


[ServerRpc] FireServerRpc(Vector2 muzzleWorld, Vector2 dir):


First call ISimApi.TryConsumeAmmo(actorId). If fired == false → early out (no ammo).


Perform current server-side hit logic (raycast/overlap) using muzzleWorld + dir (no gameplay changes this round).


Trigger VFX: see below.


Shot VFX (Presentation)
Add ShotVfxPlayer (MonoBehaviour), referenced by NetPlayer or a global registry:
PlayMuzzleFlash(Vector2 pos, float angle) (uses a pooled sprite/particle).


PlayTracer(Vector2 from, Vector2 to)


PlayImpact(Vector2 pos, float angle) (spark).


On FireServerRpc, after resolving hit, broadcast a ClientRpc carrying the minimal VFX payload (from, to, impact?) so all clients (including the owner) play the same visuals. (Keep payload small; this isn’t authoritative state.)
Note: This is VFX only; we are not adding knockback or new gameplay effects yet.

UI (UI Toolkit)
HUD reads NetPlayer.netSlots & activeIndex for the local player and updates:


Slot names: Base, Rifle, SMG, Shotgun (or None).


Ammo: number or ∞ for -1.


Active highlight on current slot.


Wire input actions for slot selection (slot0…slot3) and Interact.



Input
Ensure HellscapeControls.inputactions has:


Player/Interact (E / gamepad West).


Player/Slot0..Slot3 (keyboard 1..4 / d-pad).


Player/Fire (mouse/touch/trigger).


Player/Aim provides world position on mouse/touch (you already set this up).


NetPlayer:
Owner-only reads these actions.


For Fire, calculates aim dir = normalize(worldAim - muzzleWorld).



Removing cycles (enforce)
Delete/stop using any NetSim.Bridge‐style concrete coupling.


Add the Domain port ISimApi (above).


App implements and registers it (ServiceLocator.Register<ISimApi>(...)).


Net/Presentation only ever call ServiceLocator.Resolve<ISimApi>() and use that interface.


No adapter ever references App in asmdefs.



Files to create/modify (summary)
Domain
Weapons/WeaponType.cs (new)


Weapons/InventoryModels.cs (new)


Weapons/InventoryLogic.cs (new)


(If needed) small additions to ServerSim to expose actor registration, but do not add Unity types.


App
Services/ServiceLocator.cs (new)


Sim/SimApiAdapter.cs (new) — implements ISimApi


Installers/GameInstaller.cs (new) — registers ISimApi (and ensures ServerSim exists)


Net
NetInventoryModels.cs (new)


NetPlayer.cs (modify: inventory replication; slot/Interact/Fire RPCs; muzzle usage)


WeaponPickup.cs (new)


Presentation
WeaponPickupSpawner.cs (new)


Vfx/ShotVfxPlayer.cs (new)


HUD scripts (update to read netSlots/activeIndex)


Tests
Assets/Hellscape/Tests/EditMode/InventoryLogicTests.cs (new)



Acceptance
Host & clients see weapons spawn over time (server-controlled).


Walking to a pickup + Interact:


Same type merges ammo.


Empty slot takes it.


Inventory full: if active is base (0), replace slot1 (drop old); else replace active (drop old). Base is never dropped.


Drops appear immediately on ground (server → replicated).


Slot switching works from input and updates HUD highlight & ammo text.


Firing:


Ammo decrements on server (unless infinite).


VFX play from the muzzle position toward aim (all peers see the same).


No gameplay changes beyond existing hit logic.


No assembly cycles: Domain contains ports/DTOs; App composes; Net/Presentation depend only on Domain+Unity.




