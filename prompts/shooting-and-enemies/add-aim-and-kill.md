You are Cursor working on the Hellscape repo with `.cursorrules` loaded.
**Do NOT** run `dotnet test`. For tests, use Unity’s CLI script:
`pwsh docs/tools/unity-test.ps1 -Platform EditMode`

**Product refs:** `docs/design/gdd.md` (v0.1)
- Use §4 (Tier 0 “Damned”).
- Use §5 Effects (armor model—T0 has none).
- Use §6 Controls (mouse/touch aim, Fire button).

---

## Feature
**Players can host/join, move with camera follow, aim with mouse/touch (gun points at cursor/tap), enemies spawn at map edges and path toward players, the play zone is a walled rectangle, and players can shoot to kill.** No hard cap on players.

---

## Scope & Rules

### 1) Map bounds & walls (Domain + Presentation)
- **Authoritative bounds in Domain:** Add a rectangular playfield to `ServerSim`.
  - Rect center (0,0), half extents `(25, 14)` (tweakable).
  - Clamp all actor positions each tick: `Vector2 ClampToPlayfield(Vector2 p)`.
- **Walls (visual hint only):** Create prefab `PlayfieldWalls` with four `SpriteRenderer` strips (and optional `BoxCollider2D` just for feel). Place in scene aligned to `(±25, ±14)`.

### 2) Enemy spawner at edges (Domain + App glue)
- **ServerSim (Domain):**
  - Add `SpawnEnemiesAtEdges(int count, float inset)` → random points along four edges, spawn Tier 0 **Damned** (`hp=60`, `speed = PlayerSpeed*0.8f`, no armor).
  - Enemy AI each tick: seek nearest player; set velocity toward them (simple accel/decel or direct).
- **SimGameServer (App):**
  - On server init / first player connect, call `sim.SpawnEnemiesAtEdges(12, 1.5f)`.
  - Maintain `actorId ↔ NetEnemy` mapping; spawn/destroy views as enemies appear/disappear.
  - Each `FixedUpdate`, write `netPos` and `netHp` on each `NetEnemy`.

### 3) Input & Aiming (Input System + Net + Presentation)
**Goal:** Aim by mouse **or** touch **position** (not deltas). Gamepad uses right stick. A mobile Fire button is supported.

**3.a Update Input Actions (`Input/HellscapeControls.inputactions`)**
- Add/ensure actions (Action Map: `Player`):
  - **Move** — `Value/Vector2` (gamepad left stick, WASD, on-screen stick later).
  - **AimPos** — `PassThrough/Vector2`
    - Bindings:
      - `<Pointer>/position` (mouse)
      - `<Touchscreen>/primaryTouch/position` (touch)
  - **AimStick** — `PassThrough/Vector2`
    - Binding: `<Gamepad>/rightStick`
  - **Fire** — `Button`
    - Bindings:
      - `<Mouse>/leftButton`
      - `<Gamepad>/rightTrigger`
      - `OnScreenButton` (for mobile; we’ll add an on-screen control in scene later)
  - **Dash** — `Button` (keep existing if present)

> Implementation note: set `AimPos` and `AimStick` to **PassThrough** so they update every frame; keep processors minimal (deadzone on sticks is fine).

**3.b NetPlayer (owner input → server)**
- Compute **aim direction** each send:
  1) If `AimPos` is available this frame:
     - Read screen position (mouse/touch), convert via `Camera.main.ScreenToWorldPoint`, then:
       `aim = (worldPos - playerWorldPos).normalized`
  2) Else if `AimStick` magnitude ≥ deadzone → use its normalized value.
- Build `buttons` bitfield using `MovementConstants.AttackButtonBit` when **Fire** held.
- Send at ~20 Hz:
  ```csharp
  [ServerRpc(RequireOwnership=true)]
  void SubmitInputServerRpc(Vector2 move, Vector2 aim, byte buttons);

- INetSimBridge + SimGameServer: Canonical bridge:
  void SubmitInputFrom(ulong clientId, UnityEngine.Vector2 move, UnityEngine.Vector2 aim, byte buttons);

-**3.c Gun visual (Presentation)**

* Add child `Gun` (empty GO with `SpriteRenderer`) to the player prefab.

* New `AimVisual.cs`:

  * On **owner** only, recompute the same aim from `AimPos/AimStick` locally (visual-only) and rotate the `Gun` to face it.

  * Optional: flip sprite on Y when pointing left.

### **4\) Camera follow (Presentation)**

Create `CameraFollowOwner.cs`:

* On `NetPlayer.OnNetworkSpawn`, if `IsOwner`, set main camera to follow this transform with a light smooth damp.

### **5\) Shooting to kill (Domain authoritative)**

* `ServerSim` consumes `InputCommand`:

  * When `AttackButtonBit` set and pistol cooldown OK, perform hitscan from player position along `aim`.

    * Range \~12, hitRadius \~0.35.

    * Hit nearest enemy intersecting the ray/capsule; apply `AttackDamage(0, 20)`; set enemy `flinchTimer = 0.1s`; remove on `hp <= 0`.

* Replicate enemy hp to clients (`NetEnemy.netHp`) to flash & despawn.

### **6\) Unlimited players**

* In `RelayBootstrap`, keep `maxConnections` high (e.g., 16). NGO will spawn a `NetPlayer` per client.

---

## **Files to create/modify**

**Domain (NO UnityEngine)**

* `Assets/Hellscape/Scripts/Domain/ServerSim.cs`

  * Add fields for `playfieldHalfExtents`.

  * Add `ClampToPlayfield(Vector2 p)` and call when integrating motion.

  * Add `SpawnEnemiesAtEdges(int count, float inset)`.

  * Ensure enemies seek nearest player.

* (If not present) `Assets/Hellscape/Scripts/Domain/Combat/*`

  * `AttackDamage`, `Armor`, `DamageResolver` per GDD §5 (T0 uses no armor).

**App**

* `Assets/Hellscape/Scripts/App/INetSimBridge.cs` → signature with `(move, aim, buttons)`.

* `Assets/Hellscape/Scripts/App/SimGameServer.cs`

  * Implement the new bridge.

  * On server start/first connect, call `SpawnEnemiesAtEdges(12, 1.5f)`.

  * Push `netPos`/`netHp` for enemies each `FixedUpdate`.

**Net**

* `Assets/Hellscape/Scripts/Net/NetPlayer.cs`

  * Read `Move`, `AimPos`, `AimStick`, `Fire`; compute mouse/touch aim; send `SubmitInputServerRpc(move, aim, buttons)`.

  * Keep position replication as-is.

* `Assets/Hellscape/Scripts/Net/NetEnemy.cs` (new or extend)

  * `NetworkVariable<Vector2> netPos`

  * `NetworkVariable<short> netHp`

  * On hp change: brief color flash; on `hp<=0` → despawn/destroy.

**Presentation**

* `Assets/Hellscape/Scripts/Presentation/CameraFollowOwner.cs` (new)

* `Assets/Hellscape/Scripts/Presentation/AimVisual.cs` (new)

* Prefabs:

  * `Assets/Hellscape/Prefabs/Player.prefab`: add `Gun` child; ensure `NetworkObject`, `PlayerInput`, `NetPlayer`, `CameraFollowOwner`; put `AimVisual` on the `Gun`.

  * `Assets/Hellscape/Prefabs/NetEnemy.prefab`: `NetworkObject`, `SpriteRenderer`, `NetEnemy`.

  * `Assets/Hellscape/Prefabs/PlayfieldWalls.prefab`: four border sprites sized to `(±25, ±14)`.

* Scene:

  * Place `PlayfieldWalls` aligned to the playfield rect.

**Input**

* Modify `Input/HellscapeControls.inputactions` as described in **3.a** (AimPos, AimStick, Fire bindings).

**Relay**

* `Assets/Hellscape/Scripts/Net/RelayBootstrap.cs` → set `maxConnections` (Inspector) to 16\.

**Tests (EditMode, Domain)**

* `ServerSim_ClampWithinPlayfield()`

* `Enemy_SeeksNearestPlayer()`

* (Keep AP/Armor tests if present)

---

## **Suggested constants**

* Playfield half extents: `(25, 14)`

* Enemy (Damned): `HP = 60`, `Speed = PlayerSpeed*0.8`, `Flinch = 0.1s`

* Pistol: `Cooldown = 0.15s`, `Range = 12`, `HitRadius = 0.35`, `Damage = AttackDamage(0, 20)`

---

## **Acceptance (manual)**

1. Host via `RelayBootstrap` (get join code); start a second instance and join.

2. Each client spawns one player. **Camera follows** local player.

3. **Mouse or touch aim** rotates player’s **Gun** toward cursor/tap; move with WASD/left stick.

4. Enemies are present and **walk in from map edges** toward players.

5. **Fire** shoots; hits flinch and **kill** enemies; deaths replicate to all.

6. Players can keep joining (no strict cap enforced).

---

## **Output**

* List of created/modified files.

* Short instructions to test (Host/Join).

* Conventional Commits:

  * `feat(domain): playfield rect, edge spawner, seek AI`

  * `feat(net): owner aim+buttons RPC, enemy HP/pos replication`

  * `feat(presentation): camera follow and gun aim visual`

  * `feat(input): AimPos (mouse/touch) and AimStick bindings; Fire button`

  * `chore(relay): raise max connections`

  * `test(domain): playfield clamp and seek-nearest`