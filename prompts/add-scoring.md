You are Cursor working on the Hellscape repo with `.cursorrules` loaded.
**Do NOT** run `dotnet test`. For tests, use Unity’s CLI:
`pwsh docs/tools/unity-test.ps1 -Platform EditMode`

**Context:** Players can join, move/aim/shoot; enemies spawn/chase; players can die and revive after team survives 10s.

## Goals
1) Rename **CombatConstants.PistolCooldownTicks → PistolCooldownSeconds** (design-friendly).
2) **Score**: +1 per enemy killed. Display live score.
3) **Game Over**: when all players are dead: show final score centered. **Host** can restart the run.
4) **Revive UI**: while any player is dead, show “X awaiting revival — t=Y.Ys” to alive players.
5) **Spawner ramps**: spawn interval decreases and max enemy cap increases over elapsed time.

---

## A) Domain (pure C#)

### 1) Cooldown constant rename
- Find `CombatConstants.PistolCooldownTicks` (or any uses of pistol cooldown in ticks) and **rename** to `PistolCooldownSeconds`. Replace usages to feed **seconds** directly (no tick math) into weapon cooldown logic.

> If the project currently uses `WeaponSpec.Cooldown` (seconds), just delete the “Ticks” constant and define:
```csharp
namespace Hellscape.Domain.Combat
{
    public static class CombatConstants
    {
        public const float PistolCooldownSeconds = 0.15f; // adjust to taste
    }
}
* Update any `WeaponSpec.Pistol` factory to use `CombatConstants.PistolCooldownSeconds`.

### **2\) Team score \+ dead count access**

**File:** `Assets/Hellscape/Scripts/Domain/ServerSim.cs`

* Add an **int teamScore** field.

* When an **enemy** dies (hp \<= 0 and you remove/cull it), increment `teamScore += 1`.

* Expose reads:

public int GetTeamScore() \=\> teamScore;

public int GetDeadPlayerCount() { int d=0; foreach (var a in actors.Values) if (a.type==ActorType.Player && \!a.alive) d++; return d; }

public int GetAlivePlayerCount() { int c=0; foreach (var a in actors.Values) if (a.type==ActorType.Player && a.alive) c++; return c; }

public float GetReviveSecondsRemaining() \=\> life.ReviveSecondsRemaining; // already exists if you followed prior step

Ensure that when enemies die you actually **remove** them from `actors` (or flag them and exclude from updates) so they don’t re-trigger.

---

## **B) App/Net (authoritative bridge \+ replication \+ restart)**

### **1\) Add NetworkVariables to SimGameServer**

**File:** `Assets/Hellscape/Scripts/App/SimGameServer.cs`  
 Add to class (server-write, everyone-read):

public readonly NetworkVariable\<int\>   netTeamScore     \= new(writePerm: NetworkVariableWritePermission.Server);

public readonly NetworkVariable\<bool\>  netGameOver      \= new(writePerm: NetworkVariableWritePermission.Server);

public readonly NetworkVariable\<float\> netReviveSeconds \= new(writePerm: NetworkVariableWritePermission.Server);

public readonly NetworkVariable\<int\>   netDeadAwaiting  \= new(writePerm: NetworkVariableWritePermission.Server);

Track spawning difficulty drift:

\[SerializeField\] float baseSpawnInterval \= 3.0f;

\[SerializeField\] float minSpawnInterval  \= 0.75f;

\[SerializeField\] int   baseEnemyCap      \= 16;

\[SerializeField\] int   maxEnemyCap       \= 60;

\[SerializeField\] float rampSeconds       \= 360f; // reach full difficulty by 6 minutes

float spawnTimer;

float elapsed;

In `FixedUpdate()` **after** `sim.Tick()` and replication:

// 1\) replicate meta

netTeamScore.Value     \= sim.GetTeamScore();

netReviveSeconds.Value \= sim.GetReviveSecondsRemaining();

netDeadAwaiting.Value  \= sim.GetDeadPlayerCount();

// 2\) game over check (all players dead)

bool allDead \= (sim.GetAlivePlayerCount() \== 0);

if (allDead) netGameOver.Value \= true;

// 3\) ramped spawner (halt on game over)

elapsed \+= Time.fixedDeltaTime;

if (\!netGameOver.Value)

{

    float t \= Mathf.Clamp01(elapsed / Mathf.Max(1f, rampSeconds));

    float spawnInterval \= Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);

    int   cap           \= Mathf.RoundToInt(Mathf.Lerp(baseEnemyCap, maxEnemyCap, t));

    spawnTimer \+= Time.fixedDeltaTime;

    if (spawnTimer \>= spawnInterval && CurrentEnemyCount() \< cap)

    {

        spawnTimer \= 0f;

        SpawnEnemyAt(RandomEdgePosUnity()); // use your existing spawn path

    }

}

`CurrentEnemyCount()` should return the number of **alive** enemies (track your list/map of enemy views or query the sim for enemy actors if you have such helper).

### **2\) Restart flow (host decides)**

* Add a tiny **Restart** button / hotkey on the SimGameServer GUI when `netGameOver == true`. Only show button to **IsServer**.

* Implement `RestartRun()`:

  * Reset NVs: `netGameOver=false`, `netTeamScore=0`, timers/elapsed/spawnTimer=0

  * **Recreate** the ServerSim: `sim = new ServerSim(seed); sim.Start();`

  * For each connected client, re-register its `NetPlayer` and spawn/teleport their actors at spawn; set HP/alive to fresh values in Domain.

  * Despawn all existing **enemy** NetworkObjects (track them when spawning).

Example glue:

void OnGUI()

{

    // existing relay UI...

    if (netGameOver.Value)

    {

        var msg \= $"GAME OVER\\nFinal Score: {netTeamScore.Value}\\n";

        GUI.Label(new Rect(10, 200, 480, 80), msg);

        if (IsServer)

        {

            if (GUI.Button(new Rect(10, 280, 220, 40), "Restart Run (R)")) RestartRun();

        }

    }

}

void Update()

{

    if (IsServer && netGameOver.Value && Input.GetKeyDown(KeyCode.R)) RestartRun();

}

void RestartRun()

{

    // Despawn enemies you've spawned (keep references in a list as you spawn them)

    DespawnAllEnemies();

    // Recreate sim \+ reset counters

    sim \= new ServerSim(seed);

    sim.Start();

    elapsed \= 0f; spawnTimer \= 0f;

    netTeamScore.Value \= 0;

    netGameOver.Value  \= false;

    // Re-register players: give them actors and reset their views

    foreach (var kv in NetworkManager.ConnectedClients)

    {

        var po \= kv.Value.PlayerObject;

        var p  \= po ? po.GetComponent\<Hellscape.Net.NetPlayer\>() : null;

        if (p \!= null) RegisterNetPlayerServer(p); // spawns domain player

    }

}

*(If your `RegisterNetPlayerServer` already guards duplicates, you may need a “force rebind” path that assigns a fresh domain actor and updates the mapping dictionaries.)*

---

## **C) UI (Presentation)**

### **1\) Live score (top-left)**

Use a minimal OnGUI or a Canvas Text. Quick way:  
 **File:** `Assets/Hellscape/Scripts/Presentation/HudScore.cs`

using UnityEngine;

using Unity.Netcode;

using Hellscape.App;

public sealed class HudScore : MonoBehaviour

{

    \[SerializeField\] SimGameServer server; // assign in scene

    void OnGUI()

    {

        if (server \== null) return;

        if (\!NetworkManager.Singleton) return;

        if (\!NetworkManager.Singleton.IsServer && \!NetworkManager.Singleton.IsClient) return;

        int score \= server.netTeamScore.Value;

        float t   \= server.netReviveSeconds.Value;

        int dead  \= server.netDeadAwaiting.Value;

        GUI.Label(new Rect(10, 10, 300, 24), $"Score: {score}");

        if (dead \> 0 && t \> 0.01f)

            GUI.Label(new Rect(10, 34, 400, 24), $"{dead} awaiting revival — {t:0.0}s");

    }

}

Add this component to a GameObject in the scene and drag the `SimGameServer` reference.

### **2\) Game Over overlay (center)**

Already handled in `SimGameServer.OnGUI()` above (final score \+ Restart button for host). Clients will just see the final score and “waiting for host”.

---

## **D) Touch/mouse aim (no change needed here)**

Your current owner → aimDir pipeline is fine. No changes required for this step.

---

## **E) Output**

* New/Modified files:

  * `Domain/Combat/CombatConstants.cs` (rename or add PistolCooldownSeconds)

  * `Domain/ServerSim.cs` (teamScore; getters; enemy death increments)

  * `App/SimGameServer.cs` (NetworkVariables, ramped spawner, game over detection, restart logic)

  * `Presentation/HudScore.cs` (score \+ revive text)

  * Enemy tracking helpers for despawn on restart