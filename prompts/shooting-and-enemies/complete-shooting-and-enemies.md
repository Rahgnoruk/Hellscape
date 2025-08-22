You are Cursor working on the Hellscape repo with .cursorrules loaded.
Do NOT run `dotnet test`. For tests, use Unity’s CLI script 
- Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1

### Feature
MP pistol shooting + basic enemy (“Damned”) under thin server authority. Two players can host/join, aim, and fire pistols to kill a spawned enemy.

### Scope
- Extend networked input to include **aim** and **buttons** (attack/dash bitfield).
- Owner-only input (New Input System) → `[ServerRpc] SubmitInput(move, aim, buttons)` at 20 Hz.
- Server (`SimGameServer`) forwards to authoritative `ServerSim.ApplyForActor(actorId, InputCommand)`.
- Sim does hitscan pistol (already implemented) and HP/dying, and enemies seek players.
- Replicate enemy positions and despawn on death using `NetEnemy`.

### Changes

1) **Net API**
- `Assets/Scripts/Net/INetSimBridge.cs`
  - Replace method with:
    ```csharp
    void SubmitInputFrom(ulong clientId, UnityEngine.Vector2 move, UnityEngine.Vector2 aim, byte buttons = 0);
    ```
- `Assets/Scripts/App/SimGameServer.cs`
  - Implement the above signature (you already have an overload—use it as the canonical path).
  - Remove/redirect the old 2-arg version to the new one with `aim = Vector2.zero` for safety.
  - On server spawn/start, **spawn one test enemy**:
    ```csharp
    // e.g. near (2, 0)
    SpawnEnemyAt(new UnityEngine.Vector2(2f, 0f));
    ```

2) **NetPlayer (owner input → server)**
- `Assets/Scripts/Net/NetPlayer.cs`
  - Track `_lookInput` (from right stick) + mouse-aim direction:
    ```csharp
    UnityEngine.Vector2 ComputeAim()
    {
        var aim = _lookInput;
        #if !UNITY_WEBGL || UNITY_EDITOR
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            var mp = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            var world = UnityEngine.Camera.main != null
                ? (Vector2)UnityEngine.Camera.main.ScreenToWorldPoint(new UnityEngine.Vector3(mp.x, mp.y, 0f))
                : (Vector2)transform.position;
            var dir = world - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.0001f) aim = dir.normalized;
        }
        #endif
        // Normalize right-stick too
        if (aim.sqrMagnitude > 1f) aim = aim.normalized;
        return aim;
    }
    ```
  - Maintain an `Attack` bit (use your `MovementConstants.AttackButtonBit` = `0x01`):
    ```csharp
    byte BuildButtons()
    {
        byte b = 0;
        if (_attackHeld || _attackPressedThisTick) b |= 0x01; // Attack
        // if you wire dash later: if (_dashPressedThisTick) b |= 0x04;
        _attackPressedThisTick = false; // edge reset each tick window
        return b;
    }
    ```
  - Update the send path in `Update()`:
    ```csharp
    if (_rpcAccum >= (1f / Mathf.Max(1f, rpcRate)))
    {
        var mv = _moveInput.sqrMagnitude > 1f ? _moveInput.normalized : _moveInput;
        var aim = ComputeAim();
        var buttons = BuildButtons();
        SubmitInputServerRpc(mv, aim, buttons);
        _rpcAccum = 0f;
    }
    ```
  - Update the RPC signature:
    ```csharp
    [ServerRpc(RequireOwnership = true)]
    void SubmitInputServerRpc(Vector2 move, Vector2 aim, byte buttons)
    {
        NetSim.Bridge?.SubmitInputFrom(OwnerClientId, move, aim, buttons);
    }
    ```
  - In the Input callbacks:
    - `OnLook` → `_lookInput = ctx.ReadValue<Vector2>();`
    - `OnAttack` → set `_attackHeld` and `_attackPressedThisTick` on performed/canceled.

3) **Domain hook (already present)**
- `ServerSim.ApplyForActor(actorId, InputCommand)` should accept the `aim` and `buttons` (it already does via `InputCommand`).
- Pistol constants are in `CombatConstants.cs` and used by shooting; keep as-is.

4) **Enemy replication**
- `Assets/Scripts/Net/NetEnemy.cs`
  - Ensure it has `NetworkVariable<Vector2> netPos` and a `ServerRpc` to despawn on death (you already have a pattern).
- `Assets/Scripts/App/SimGameServer.cs`
  - You already push `actorToNetEnemy` positions per FixedUpdate and despawn when the sim says the actor is gone. Keep/update that loop.

5) **Prefabs & Scene**
- Assign `SimGameServer.netEnemyPrefab` in the scene to `Assets/Prefabs/NetEnemy.prefab`.
- Confirm `NetworkManager` has **Player Prefab** set to your `Player.prefab` (which contains `NetworkObject` + `NetPlayer`).

6) **Minimal tests**
- Add/keep a simple round-trip test for `InputCommand` serialization in `Hellscape.Tests` if you introduce any new blittable DTOs.
- Don’t try to unit test NGO handshakes—compile + the manual acceptance test is enough here.

### Acceptance
- Host via Relay, get join code, Client joins.
- Server spawns at least one Basic enemy.
- Both players can **aim with mouse/right stick** and **LMB/Attack** to fire pistols.
- Enemy takes damage and **dies**; despawns on all clients.
- Movement remains smooth for both players.

### Files to touch
- `Assets/Scripts/Net/INetSimBridge.cs`
- `Assets/Scripts/App/SimGameServer.cs`
- `Assets/Scripts/Net/NetPlayer.cs`
- (optional) `Assets/Scripts/Net/NetEnemy.cs` if HP/despawn glue is missing

### Output
- List of created/modified files.
- Short host/join testing steps.
- Suggested Conventional Commits.
