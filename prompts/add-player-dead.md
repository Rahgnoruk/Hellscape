You are Cursor working on the Hellscape repo with `.cursorrules` loaded.
**Do NOT** run `dotnet test`. For tests, use Unity’s CLI:
- Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1

**Context:** Players can already host/join, move/aim, shoot; enemies chase and take damage; shots have VFX.
**Goal:** 
1) Enemies keep spawning (trickle under a cap).
2) Players have HP and can die. If any player dies, a **shared 10s timer** starts; if the remaining players **survive the full 10s**, all dead players **respawn**. If another player dies during the countdown, the timer **resets to 10s**. Team wipe = lose (flag only for now).

---

## A) Domain — Lives System + Player HP + Enemy contact damage

### 1) Lives/Respawn system (pure C#)
**File (new):** `Assets/Hellscape/Scripts/Domain/Lives/LifeSystem.cs`
```csharp
namespace Hellscape.Domain
{
    // Tracks dead players and a shared survive-to-revive countdown.
    public sealed class LifeSystem
    {
        private readonly System.Collections.Generic.HashSet<int> dead = new();
        private float reviveCountdown = -1f; // <0 means idle
        private readonly float reviveDuration;

        // buffer for respawns after Tick
        private readonly System.Collections.Generic.List<int> toRespawn = new();

        public LifeSystem(float reviveDurationSeconds = 10f) { reviveDuration = reviveDurationSeconds; }

        public bool IsDead(int actorId) => dead.Contains(actorId);
        public void MarkDead(int actorId)
        {
            if (dead.Add(actorId))
                reviveCountdown = reviveDuration; // (re)start countdown when the first death happens or when a new one dies
        }

        public void MarkAlive(int actorId) { dead.Remove(actorId); if (dead.Count == 0) reviveCountdown = -1f; }

        public void Tick(float deltaTime, int alivePlayerCount)
        {
            toRespawn.Clear();
            if (alivePlayerCount <= 0) { reviveCountdown = -1f; return; } // team wipe; handled by ServerSim
            if (dead.Count == 0) { reviveCountdown = -1f; return; }
            if (reviveCountdown < 0f) reviveCountdown = reviveDuration;

            reviveCountdown -= deltaTime;
            if (reviveCountdown <= 0f)
            {
                // Everyone who is dead comes back now
                foreach (var id in dead) toRespawn.Add(id);
                dead.Clear();
                reviveCountdown = -1f;
            }
        }

        public System.Collections.Generic.IReadOnlyList<int> ConsumeRespawns()
        {
            return toRespawn.ToArray();
        }

        public float ReviveSecondsRemaining => reviveCountdown < 0f ? 0f : reviveCountdown;
        public int DeadCount => dead.Count;
    }
}
### **2\) Player HP, death, enemy contact damage**

**File:** `Assets/Hellscape/Scripts/Domain/ServerSim.cs`

* Add a `LifeSystem` field and initialize it in `Start()`.

* Ensure **players** and **enemies** both have `hp` (players start \~100).

* When **hp \<= 0**:

  * if player → mark dead in `LifeSystem`, flag actor as dead (no input, no movement), set velocity zero.

  * if enemy → remove later as you already do.

* **Contact damage** from enemies:

  * Each enemy has `attackCooldown` and deals e.g. `10` damage if within `attackRange = 0.75f`.

  * On hit, reset `attackCooldown` (e.g., `0.8f`).

* **Ignore inputs** for dead players in `ApplyForActor`.

* After advancing simulation each tick:

  * Call `life.Tick(deltaTime, alivePlayers)` and **respawn** any returned actor IDs at safe spawn points (e.g., near outskirts or near the nearest alive player). On respawn: restore `hp = 60–100`, clear dead flag, small invuln window optional (skip for now).

* Expose a public read for the revive timer (so App can replicate/UI): `public float GetReviveSecondsRemaining()`.

**Sketch of changes (add/modify in ServerSim):**

private LifeSystem life;

public void Start() {

    // ...

    life \= new LifeSystem(10f);

    // spawn players as before

}

public void Tick(float deltaTime) {

    tick++;

    // 1\) Apply inputs only if actor is alive

    foreach (var kv in \_latestByActor) {

        if (actors.TryGetValue(kv.Key, out var a) && a.type \== ActorType.Player && a.alive)

            a.ApplyInput(kv.Value, /\* speeds \*/, deltaTime);

    }

    // 2\) Enemy AI \+ contact damage

    foreach (var a in actors.Values) {

        a.Tick(deltaTime);

        if (a.type \== ActorType.Enemy && a.alive) EnemyAttackPlayers(ref a, deltaTime);

    }

    // 3\) Lives ticking

    int alivePlayers \= CountAlivePlayers();

    life.Tick(deltaTime, alivePlayers);

    // 4\) Respawns

    var rs \= life.ConsumeRespawns();

    foreach (var id in rs) RespawnPlayer(id);

    // 5\) cull dead enemies if hp\<=0 (existing logic)

}

private int CountAlivePlayers() { int c=0; foreach (var a in actors.Values) if (a.type==ActorType.Player && a.alive) c++; return c; }

private void EnemyAttackPlayers(ref Actor enemy, float dt) {

    enemy.attackCd \-= dt;

    if (enemy.attackCd \> 0f) return;

    const float attackRange \= 0.75f;

    const short damage \= 10;

    // find nearest alive player within range

    int targetId \= \-1; float bestDistSq \= 999999f;

    foreach (var kv in actors) {

        var p \= kv.Value;

        if (p.type \!= ActorType.Player || \!p.alive) continue;

        var dx \= p.pos.x \- enemy.pos.x; var dy \= p.pos.y \- enemy.pos.y;

        var dsq \= dx\*dx \+ dy\*dy;

        if (dsq \< bestDistSq) { bestDistSq \= dsq; targetId \= kv.Key; }

    }

    if (targetId \>= 0 && bestDistSq \<= attackRange\*attackRange) {

        var player \= actors\[targetId\];

        if (player.alive) {

            // simple normal damage (no armor for players yet)

            player.hp \= (short)System.Math.Max(0, player.hp \- damage);

            if (player.hp \<= 0\) {

                player.alive \= false;

                life.MarkDead(player.id);

                player.vel \= Vector2.zero;

            }

            actors\[targetId\] \= player;

            enemy.attackCd \= 0.8f;

        }

    }

}

private void RespawnPlayer(int actorId) {

    if (\!actors.TryGetValue(actorId, out var p)) return;

    p.alive \= true;

    p.hp \= 80; // respawn health

    p.pos \= SafeRespawnPosition();

    p.vel \= Vector2.zero;

    // clear dash cd etc. if needed

    actors\[actorId\] \= p;

}

private Vector2 SafeRespawnPosition() {

    // reuse outskirts or near edge helper if present; otherwise pick (-10, 6\)

    return new Vector2(-10f, 6f);

}

public float GetReviveSecondsRemaining() \=\> life.ReviveSecondsRemaining;

* **Actor struct adjustments** (inside `ServerSim.Actor`):

  * Add `public bool alive = true;`

  * Add `public float attackCd;` for enemies.

  * Ensure `ToActorState()` includes hp and type (you already have); optionally extend snapshot later with alive flag if needed by clients.

---

## **B) Net/App — Replicate player HP/alive, show countdown, server spawner**

### **1\) NetPlayer replication hooks**

**File:** `Assets/Hellscape/Scripts/Net/NetPlayer.cs`

* Add:

public readonly NetworkVariable\<short\> netHp \= new(writePerm: NetworkVariableWritePermission.Server);

public readonly NetworkVariable\<bool\> netAlive \= new(writePerm: NetworkVariableWritePermission.Server);

* In `Update()`, if owner and `!netAlive.Value`, ignore movement input (don’t send move ServerRpc). You can still send aim for spectators if desired, but simplest is to skip all input while dead.

### **2\) SimGameServer: write player hp/alive each tick, show revive timer, top-up spawner**

**File:** `Assets/Hellscape/Scripts/App/SimGameServer.cs`

* You already replicate enemies. Add **player replication** after `sim.Tick()`:

// push player vars

foreach (var kvp in actorToNetPlayer)

{

    var actorId \= kvp.Key; var view \= kvp.Value;

    if (view \== null || \!view.IsSpawned) continue;

    if (sim.TryGetActorState(actorId, out var st))

    {

        view.netPos.Value \= new UnityEngine.Vector2(st.positionX, st.positionY);

        view.netHp.Value  \= st.hp;

        // naive alive from hp\>0 (Domain also tracks); you can expose a TryGetActorAlive if needed

        view.netAlive.Value \= (st.hp \> 0);

    }

}

**Revive timer broadcast (optional HUD):** expose a `public float reviveSeconds` value in `SimGameServer` by polling `sim.GetReviveSecondsRemaining()` and draw minimal OnGUI (we already have a debug UI). For now:

void OnGUI()

{

    // ... existing Relay UI

    if (IsServer || IsClient)

    {

        float t \= sim \!= null ? sim.GetReviveSecondsRemaining() : 0f;

        if (t \> 0.01f)

            GUI.Label(new Rect(10, 110, 320, 30), $"Revive in: {t:0.0}s (stay alive\!)");

    }

}

**Continuous spawner:** if you haven’t already, keep/enhance the top-up logic:

\[SerializeField\] int enemyCap \= 28;

\[SerializeField\] float spawnInterval \= 2.5f;

float spawnTimer;

// after replication in FixedUpdate

spawnTimer \+= Time.fixedDeltaTime;

if (spawnTimer \>= spawnInterval && actorToNetEnemy.Count \< enemyCap)

{

    spawnTimer \= 0f;

    var dpos \= sim.GetRandomEdgePositionForBridge(1.2f);

    SpawnEnemyAt(new UnityEngine.Vector2(dpos.x, dpos.y));

}

NOTE: Team wipe: if all players dead (sim reports alivePlayers=0), you can set a `bool teamWipe` flag and stop spawns. Keep simple for now.

---

## **C) Minimal presentation (dead feedback)**

* Tint `NetPlayer` sprite when dead (gray) and optionally lower alpha.

* Optional: draw a skull icon over dead players.

* Camera: dead owner’s camera can just keep following their body; later we can switch to nearest alive.

---

## **D) Tests (EditMode, Domain) — LifeSystem only**

**File (new):** `Assets/Hellscape/Tests/EditMode/LifeSystem_Tests.cs`

using NUnit.Framework;

using Hellscape.Domain;

public class LifeSystem\_Tests

{

    \[Test\]

    public void RespawnsAfter10sIfOthersAlive()

    {

        var life \= new LifeSystem(10f);

        life.MarkDead(1);

        // one alive on team

        for (int i=0;i\<10;i++) life.Tick(1f, alivePlayerCount: 1);

        var rs \= life.ConsumeRespawns();

        Assert.AreEqual(1, rs.Count);

        Assert.AreEqual(1, rs\[0\]);

    }

    \[Test\]

    public void ResetsTimerIfAnotherDies()

    {

        var life \= new LifeSystem(10f);

        life.MarkDead(1);

        life.Tick(5f, 1);

        // another player dies → timer restarts implicitly by MarkDead

        life.MarkDead(2);

        for (int i=0;i\<9;i++) life.Tick(1f, 1);

        // not yet, needs full 10s since last death

        var rs \= life.ConsumeRespawns();

        Assert.AreEqual(0, rs.Count);

        life.Tick(1f, 1);

        rs \= life.ConsumeRespawns();

        Assert.AreEqual(2, rs.Count);

    }

    \[Test\]

    public void TeamWipeStopsCountdown()

    {

        var life \= new LifeSystem(10f);

        life.MarkDead(1);

        // no alive players → countdown halts

        for (int i=0;i\<20;i++) life.Tick(1f, alivePlayerCount: 0);

        var rs \= life.ConsumeRespawns();

        Assert.AreEqual(0, rs.Count);

    }

}

## **Acceptance**

* Host \+ client join. Enemies continue to spawn up to the cap.

* Enemies deal contact damage; players have HP.

* When a player dies, they fall inert; remaining players see a **Revive in X.Xs** countdown.

* If someone else dies, timer resets to **10.0s**.

* If at least one player survives the full 10s, **all dead players respawn** with partial HP near a safe spot. Everyone sees it.

* If **all** players die at once, team-wipe flag is set (we can add a lose screen later).