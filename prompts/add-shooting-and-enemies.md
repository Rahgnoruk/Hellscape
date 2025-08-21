Use .cursorrules and docs/design/gdd.md. Do NOT run `dotnet test`; to run tests use:
  - Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1

### Feature
Authoritative SHOOTING (semi-auto hitscan pistol) + Basic ENEMY (Chaser). ServerSim owns all logic; clients only send input (move, aim, fire). Positions and deaths replicate via existing SimGameServer → Net views.

### Domain (no UnityEngine)
Add to Hellscape.Domain:

1) Types & params
- enum Team { Player=0, Enemy=1 }
- Actor gains: Team team; float radius (default Player 0.45f, Enemy 0.5f).
- Weapon constants:
  PistolDamage=25, PistolRange=12f, PistolCooldownTicks=8 (@50Hz ≈ 0.16s)
- Buttons: AttackButtonBit already exists (0x01).

2) Shooting (hitscan, deterministic)
- Each player-actor has `gunCooldownTicks` int.
- On InputCommand where AttackButtonBit is set and `gunCooldownTicks==0` and aim dir ≠ zero:
  - Normalize aim = (aimX,aimY).
  - Ray from actor.pos toward aim, length = PistolRange.
  - Find FIRST enemy center whose distance to the ray segment ≤ enemy.radius AND whose projection is within [0,range].
  - Apply PistolDamage, set `gunCooldownTicks = PistolCooldownTicks`.
  - Emit `DomainEvent.HitLanded { attackerId, targetId, dmg }`.
  - If hp<=0: remove enemy; emit `DomainEvent.ActorDied { targetId }`.

3) Chaser enemy (very simple AI)
- Each Enemy actor steers toward nearest Player within SenseRange=20f:
  - targetVel = normalized(to player) * EnemySpeed (e.g., 3.5 m/s)
  - accel/decay same helpers as player (use MovementConstants accel/decel for now).
- If no player found, slow to stop.

4) Events ring buffer in ServerSim
- Add queue/ring: `EnqueueEvent(DomainEvent e)` during Tick; `bool TryDequeueEvent(out DomainEvent e)` after.
- Define `struct DomainEvent { enum Kind { HitLanded, ActorDied } ... }`

5) Public helpers
- `int SpawnEnemyAt(Vector2 pos)` and optionally `int[] SpawnEnemiesCircle(int count, float r)` for tests.
- `bool TryGetActorState(int id, out ActorState state)` (already present).
- Keep Domain free of UnityEngine.

### Tests (Edit Mode, write first)
Create in Assets/Hellscape/Scripts/Tests/Combat/:

- `Pistol_FirstEnemyAlongRay_TakesDamage`:
  Player at (0,0), Enemy at (6,0), aim=(1,0): after one Attack command → enemy hp -= 25.
- `Pistol_Cooldown_BlocksRapidFire`:
  Two Attack commands on consecutive ticks → only one hit until cooldown passes.
- `Pistol_RangeAndMiss_NoDamage`:
  Enemy at (13,0) (>range) or at (5,2) (outside radius), aim=(1,0) → no damage.
- `Enemy_Chaser_MovesTowardPlayer`:
  Enemy at (5,0), Player at (0,0): after T ticks, enemy.x decreases; distance smaller than start.
- `Death_EmitsActorDiedAndRemoves`:
  Reduce enemy to 0 hp → event dequeued and TryGetActorState(id) == false next tick.

Use IClock.FixedDelta=0.02f in tests. Use deterministic seeds.

### App/Net glue
- In SimGameServer.FixedUpdate() after sim.Tick():
  - While `sim.TryDequeueEvent(out e)`:
    - If HitLanded: (optional) Debug.Log
    - If ActorDied and actor was mapped to a NetEnemy view, despawn it (server-side) and remove from dictionary.
  - Continue to write netPos for all mapped actors each tick.

- Create a **NetEnemy** prefab (NetworkObject + NetEnemy.cs + SpriteRenderer) mirroring NetPlayer’s view path:
  - NetEnemy.cs has: `NetworkVariable<Vector2> netPos` (server write), optional `NetworkVariable<int> actorId`.
  - SimGameServer keeps a `Dictionary<int, NetEnemy>` (actorId→view). On `SpawnEnemyAt`, instantiate prefab, set actorId, `Spawn()`, and keep it mapped. Each tick set `view.netPos.Value` from sim.TryGetActorState.

### Acceptance
- Host + one client can move and **shoot**. Hitting an enemy reduces its HP on server; when HP ≤ 0, enemy despawns on all clients.
- Chaser moves toward the nearest player.
- All logic resides in Domain; Unity used only for views and input.

### Files & placement
- Domain: `Assets/Hellscape/Scripts/Domain/Combat/` (hitscan resolver, events), minor edits to ServerSim & Actor.
- Tests: `Assets/Hellscape/Scripts/Tests/Combat/`
- Net: `Assets/Hellscape/Scripts/Net/NetEnemy.cs`
- Prefab: `Assets/Hellscape/Prefabs/NetEnemy.prefab`

### Output
- File list & diffs.
- Test results summary.
- Short instructions: where to drag `NetEnemy.prefab` into SimGameServer (serialized field) and how to verify in play.
