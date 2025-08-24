You are Cursor working on the Hellscape repo with `.cursorrules` loaded.
**Do NOT** run `dotnet test`. For tests, use Unity’s CLI:
- Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1

# Feature package: “Horde Slice”
Refactor to the new GDD: wave-based survival on a large arena, base gun + floor weapons, 2–16 players, per-player score, revive at base, scalable spawns.

## Scope Overview (keep hex!)
- **Domain (pure C#):** Combat model (ablative armor), Weapon specs, PlayerInventory (base gun + 4 slots), ScoreSystem (per-client kills), WaveDirector (time-based), Pickups (weapon+ammo), ReviveAtBase rules.
- **Net/App:** ServerSim authoritative; replicate minimal state (scores, game over, wave index, revive countdown). Server spawns/despawns pickups & enemies, validates pickups & revives. NGO + Relay unchanged.
- **Presentation:** Simple arena (walled rectangle), BaseArea trigger, PickupView, HUD (slots/ammo/scoreboard), Game Over overlay, basic muzzle/tracer VFX (already have bullet visuals—reuse).

---

## A) Domain (new/changes)

### 1) Combat constants (seconds)
Create/ensure:
`Assets/Hellscape/Scripts/Domain/Combat/CombatConstants.cs`
```csharp
namespace Hellscape.Domain.Combat {
    public static class CombatConstants {
        public const float PistolCooldownSeconds = 0.15f; // base gun (infinite ammo)
        public const float RifleCooldownSeconds  = 0.4f;
        public const float SmgCooldownSeconds    = 0.08f;
        public const float ShotgunCooldownSeconds= 0.75f;
        public const float ShotgunPellets        = 8f;
        // damage baselines
        public const float PistolDamage = 6f;
        public const float RifleTrue    = 3f, RifleNormal = 5f; // AP-ish split
        public const float SmgDamage    = 4f;
        public const float ShotgunPelletDamage = 2f; // × pellets, close-range stacks
    }
}
Remove any prior “Ticks” pistol cooldown and convert to seconds usage.

2\) Armor model (ablative per hit)

namespace Hellscape.Domain.Combat {

    public struct Armor {

        public float BlockPerHit;

        public float Durability;

        public Armor(float blockPerHit, float durability) { BlockPerHit \= blockPerHit; Durability \= durability; }

        public float Apply(ref float trueDmg, ref float normalDmg){

            float blocked \= normalDmg;

            if (blocked \> BlockPerHit) blocked \= BlockPerHit;

            if (blocked \> Durability) blocked \= Durability;

            Durability \-= blocked;

            normalDmg \-= blocked;

            return trueDmg \+ normalDmg; // return health damage

        }

    }

    public struct DamagePayload { public float True, Normal; public DamagePayload(float t,float n){True=t;Normal=n;} }

}

**Unit tests** (EditMode, Domain):

* `ArmorBlocksUpToPerHitAndDurability` (covers examples in GDD).

* `NoArmorTakesAllDamage`.

### **3\) Weapons & inventory (base \+ 4 slots)**

namespace Hellscape.Domain.Combat {

    public enum WeaponId : byte { BaseGun, Pistol, Rifle, Smg, Shotgun, Crossbow, AssaultRifle /\* future \*/, GooThrower, NetGun, FlameThrower }

    public struct WeaponSpec {

        public WeaponId id; public float cooldown; public DamagePayload damage; public bool cone; public int pellets; public bool armorPiercing;

        public WeaponSpec(WeaponId id, float cd, DamagePayload dmg, bool cone=false, int pellets=1, bool ap=false){

            this.id=id; cooldown=cd; damage=dmg; this.cone=cone; this.pellets=pellets; armorPiercing=ap;

        }

    }

    public static class WeaponDB {

        public static WeaponSpec BaseGun \=\> new(WeaponId.BaseGun, CombatConstants.PistolCooldownSeconds, new DamagePayload(0, CombatConstants.PistolDamage));

        public static WeaponSpec Pistol  \=\> new(WeaponId.Pistol, 0.20f, new DamagePayload(0,6));

        public static WeaponSpec Rifle   \=\> new(WeaponId.Rifle, CombatConstants.RifleCooldownSeconds, new DamagePayload(CombatConstants.RifleTrue, CombatConstants.RifleNormal), ap:true);

        public static WeaponSpec Smg     \=\> new(WeaponId.Smg, CombatConstants.SmgCooldownSeconds, new DamagePayload(0, CombatConstants.SmgDamage));

        public static WeaponSpec Shotgun \=\> new(WeaponId.Shotgun, CombatConstants.ShotgunCooldownSeconds, new DamagePayload(0, CombatConstants.ShotgunPelletDamage), cone:true, pellets:(int)CombatConstants.ShotgunPellets);

    }

    public sealed class PlayerInventory {

        // slot0 \= BaseGun infinite; slots 1..4 \= pickups with ammo

        public readonly struct Slot { public readonly WeaponId id; public readonly int ammo; public Slot(WeaponId id,int ammo){this.id=id;this.ammo=ammo;} public Slot WithAmmo(int a)=\>new(id,a); }

        private Slot slot0, slot1, slot2, slot3, slot4; private byte active \= 0;

        public PlayerInventory(){ slot0 \= new Slot(WeaponId.BaseGun, int.MaxValue); }

        public byte ActiveSlot \=\> active;

        public Slot Get(byte s)=\> s switch {0=\>slot0,1=\>slot1,2=\>slot2,3=\>slot3,4=\>slot4,\_=\>slot0};

        public void SetActive(byte s){ if (s\<=4) active=s; }

        public bool TryPickup(WeaponId id, int ammo){

            // stack if same weapon exists or place in first empty among 1..4

            ref Slot s1 \= ref slot1; ref Slot s2=ref slot2; ref Slot s3=ref slot3; ref Slot s4=ref slot4;

            if (s1.id==id && s1.ammo\<int.MaxValue){ s1 \= s1.WithAmmo(s1.ammo+ammo); return true; }

            if (s2.id==id && s2.ammo\<int.MaxValue){ s2 \= s2.WithAmmo(s2.ammo+ammo); return true; }

            if (s3.id==id && s3.ammo\<int.MaxValue){ s3 \= s3.WithAmmo(s3.ammo+ammo); return true; }

            if (s4.id==id && s4.ammo\<int.MaxValue){ s4 \= s4.WithAmmo(s4.ammo+ammo); return true; }

            if (s1.id==0){ s1 \= new Slot(id, ammo); return true; }

            if (s2.id==0){ s2 \= new Slot(id, ammo); return true; }

            if (s3.id==0){ s3 \= new Slot(id, ammo); return true; }

            if (s4.id==0){ s4 \= new Slot(id, ammo); return true; }

            return false;

        }

        public bool ConsumeAmmoOfActiveOneShotIfNeeded(){

            if (active==0) return true; // base gun unlimited

            ref Slot s \= ref slot1; if (active==2) s=ref slot2; else if (active==3) s=ref slot3; else if (active==4) s=ref slot4;

            if (s.ammo\<=0) return false; s \= s.WithAmmo(s.ammo-1); return true;

        }

    }

}

**Tests:**

* `PickupStacksOrFillsNewSlot`.

* `ActiveSlotConsumesAmmoExceptBaseGun`.

### **4\) Pickups and waves**

namespace Hellscape.Domain.Director {

    public sealed class WaveDirector {

        public int WaveIndex { get; private set; }

        public float Elapsed { get; private set; }

        public void Tick(float dt){ Elapsed \+= dt; WaveIndex \= 1 \+ (int)(Elapsed / 30f); } // simple: \+1 wave / 30s

        public float SpawnInterval() \=\> UnityEngine.Mathf.Lerp(3.0f, 0.6f, UnityEngine.Mathf.Clamp01(Elapsed/360f));

        public int EnemyCap() \=\> (int)UnityEngine.Mathf.Lerp(18, 80, UnityEngine.Mathf.Clamp01(Elapsed/480f));

        // (optional) return weighted tables by WaveIndex for types; simple for now.

    }

}

Add minimal domain description for **Pickup** (DTO):

using Hellscape.Domain.Combat;

namespace Hellscape.Domain.Loot {

    public struct PickupDTO { public int id; public WeaponId weapon; public int ammo; public float x,y; }

}

### **5\) Score system \+ revive-at-base**

In `ServerSim`:

* Track `Dictionary<ulong,int> killCounts` (by attacker clientId).

* Expose getters: `GetKills(ulong clientId)`, `EnumerateScores()`, `GetTeamScore() => sum`.

* **ReviveAtBase rule:** if any player is dead and at least one *alive* player enters BaseArea, start a revive countdown (e.g., 10s). On complete → respawn all dead at base with baseline HP.

Add a `SetBaseArea(Rect rect)` API and a `NotifyPlayerEnteredBase(ulong clientId,bool inside)` (server toggles per-frame based on Presentation overlap checks).

**Tests:**

* `ArmorExamplesMatchDoc()`

* `WaveDirectorRamps()`

* `InventoryAmmoConsumption()`

---

## **B) Net/App (authoritative glue)**

### **1\) Player count**

In `RelayBootstrap.cs`, expose `maxConnections` as **\[SerializeField\] int \= 15** to support 2–16 players (host \+ 15 clients). No hard limit logic elsewhere.

### **2\) Network replication (SimGameServer.cs)**

Add server-write NVs:

public readonly NetworkVariable\<int\>   netWaveIndex     \= new(writePerm: Server);

public readonly NetworkVariable\<int\>   netTeamScore     \= new(writePerm: Server);

public readonly NetworkVariable\<bool\>  netGameOver      \= new(writePerm: Server);

public readonly NetworkVariable\<float\> netReviveSeconds \= new(writePerm: Server);

Add a `NetworkList<PlayerScore>` for per-player kills:

public struct PlayerScore : INetworkSerializable {

    public ulong clientId; public int kills;

    public void NetworkSerialize\<T\>(BufferSerializer\<T\> s) where T: IReaderWriter { s.SerializeValue(ref clientId); s.SerializeValue(ref kills); }

}

public NetworkList\<PlayerScore\> netScores;

* On server start, instantiate `netScores = new(NetworkVariableReadPermission.Everyone);`

* Each tick after `sim.Tick()`:

  * `netWaveIndex.Value = waveDirector.WaveIndex;`

  * `netTeamScore.Value = sim.GetTeamScore();`

  * Update `netReviveSeconds.Value = sim.GetReviveSecondsRemaining();`

  * Rebuild `netScores` from domain kills if changed (avoid per-frame churn—update when diff).

### **3\) Pickups: spawning \+ claiming**

Server spawns **PickupView** NetworkObjects at intervals (weighted by wave). On client overlap, owner calls `[ServerRpc] TryPickupServerRpc(pickupNetId)`; server validates distance & availability; if OK → `inventory.TryPickup()` and despawns the pickup NO.

### **4\) Revive at base**

Add `BaseArea` MonoBehaviour (Presentation) that tells server who is inside via a lightweight `ClientRpc` or per-frame server overlap checks (server has authoritative transforms already; simplest: BaseArea registers itself and server checks players’ positions).  
 When condition met and dead players exist → start 10s revive timer; update `netReviveSeconds`; on finish → respawn dead at base.

### **5\) Game over**

When `AlivePlayers == 0` → `netGameOver = true`, freeze spawns, show overlay; host can **Restart** (button / R key): reset sim, scores, wave director, clear enemies & pickups, respawn everyone at spawn points.

---

## **C) Presentation**

### **1\) Arena \+ base**

* Create a large **WalledArena** (tilemap or simple sprites) with BoxCollider2D walls.

* Add `BaseArea` (BoxCollider2D trigger) near one corner, visible decal (“BASE”). Provide a serialized Rect to server via `SimGameServer.RegisterBaseArea(rect)` at Start.

### **2\) PickupView**

`Assets/Hellscape/Scripts/Presentation/PickupView.cs`:

* `NetworkObject` \+ SpriteRenderer (weapon icon), small floating anim.

* On local player overlap, show “E: Pick up \<weapon\> (+ammo)”.

* Owner input “Use” sends `TryPickupServerRpc`.

### **3\) HUD**

* **Ammo/Slots row**: “1: BaseGun ∞ | 2: Pistol 24 | 3: Rifle 18 | 4: SMG 90 | 5: Shotgun 12”. Active slot highlighted. Keys 1–5 / wheel cycle. (Also expose actions for mobile.)

* **Scores**: top-left team score; TAB shows per-player table from `netScores`.

* **Wave**: top-center “Wave N”.

* **Revive**: if `netReviveSeconds>0`, show “Revive in X.Xs (stay at Base)”.

* **Game Over**: centered final score; host sees “Restart”.

---

## **D) Input**

Extend InputActions:

* `NextWeapon` / `PrevWeapon`, `Slot1..Slot5`.

* `Use` picks up when in range; also used for revive interaction if needed.

* For mobile, ensure `AimPosition` supports touch position (already planned); `Fire` mapped to a touch button.

---

## **E) Acceptance Checklist**

* Up to 16 players can join (host \+ 15). Camera follows owner.

* Enemies spawn continuously with **ramping interval and cap**. Wave index increments \~ every 30s.

* Pickups appear on the ground over time; players can **pick up** into slots 1–4; ammo decreases on fire; base gun is unlimited.

* Per-player **kills** tracked; team score \= sum; HUD displays both.

* If all players die → **Game Over** overlay \+ final score; host can restart.

* If any player(s) dead but an alive player stands in **BaseArea**, revive countdown shows; at 0s all dead respawn at base.

* Armor model follows GDD examples (unit tests pass).

---

## **F) Files to add/modify (guidance)**

**Domain**

* `Combat/CombatConstants.cs` (new or updated)

* `Combat/Armor.cs` (new)

* `Combat/Weapons.cs` (new: WeaponDB, PlayerInventory)

* `Director/WaveDirector.cs` (new)

* `Loot/PickupDTO.cs` (new)

* `ServerSim.cs` (extend: inventory per player, kills, pickups, waves, revive-at-base)

**Net/App**

* `App/SimGameServer.cs` (NVs, NetworkList scores, spawn ramps, revive, restart, pickups)

* `Net/` add pickup ServerRpc handlers if you centralize them

**Presentation**

* `Presentation/PickupView.cs` (new)

* `Presentation/Hud.cs` (extend/replace HudScore to show slots, wave, score, revive, game over)

* `Presentation/BaseArea.cs` (new, reports rect or registers with SimGameServer)

**Tests (EditMode)**

* `ArmorTests.cs`

* `InventoryTests.cs`

* `WaveDirectorTests.cs`

## **Notes**

* Keep gameplay rules in Domain; MonoBehaviours only visualize and forward inputs.

* Bullets already attribute damage: ensure they carry OwnerClientId so kills map to the right client.

* For shotgun, server fires N pellets with cone spread (deterministic rng seeded from tick \+ actor id).
