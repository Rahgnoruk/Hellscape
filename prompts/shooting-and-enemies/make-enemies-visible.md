You are Cursor working on the Hellscape repo with `.cursorrules` loaded.
**Do NOT** run `dotnet test`. For tests, use Unity’s CLI script:
- Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1

**Goal:** Enemies visibly spawn at the playfield edges and move toward players. Their positions/HP replicate to all clients.

---

## Scope

We’ll:
1) Add edge-spawn helpers and public enemy spawn API in **Domain/ServerSim**.
2) Spawn **NetEnemy** views from **SimGameServer** whenever we spawn domain enemies.
3) Replicate enemy `netPos` and `netHp` each tick to clients.
4) (Optional) Keep a small top-up spawner under a cap.

Keep hex boundaries: Domain stays Unity-free; Presentation/Net handles visuals.

---

## Changes

### A) Domain — ServerSim (edge spawn + public enemy spawn)

**File:** `Assets/Hellscape/Scripts/Domain/ServerSim.cs`

1. **Playfield edges helper** (if not present), plus a public bridge method:
   - Add (or reuse) a `playfieldHalfExtents` field (e.g., `(25, 14)`).
   - Implement a private helper that returns a random point on one of the four edges.
   - Expose a public wrapper for App to call.

```csharp
// Add near other fields
private Vector2 playfieldHalfExtents = new Vector2(25f, 14f);

// PRIVATE: random edge spawn inside playfield, with small inset
private Vector2 GetRandomEdgePosition(float inset)
{
    // inset clamps inside bounds to avoid spawning exactly on walls
    float hx = playfieldHalfExtents.x - inset;
    float hy = playfieldHalfExtents.y - inset;
    int side = rng.Range(0, 4); // 0=left,1=right,2=bottom,3=top
    switch (side)
    {
        case 0: return new Vector2(-hx, rng.Range((int)-hy, (int)hy + 1));
        case 1: return new Vector2( hx, rng.Range((int)-hy, (int)hy + 1));
        case 2: return new Vector2(rng.Range((int)-hx, (int)hx + 1), -hy);
        default:return new Vector2(rng.Range((int)-hx, (int)hx + 1),  hy);
    }
}

// PUBLIC: app/bridge access (keeps one source of truth)
public Vector2 GetRandomEdgePositionForBridge(float inset) => GetRandomEdgePosition(inset);
**Public enemy spawn** (domain-side):

// PUBLIC: spawn a Tier-0 enemy at world position; returns actorId  
public int SpawnEnemyAt(Vector2 pos)  
{  
    var id \= nextId++;  
    actors\[id\] \= new Actor(id, pos, ActorType.Enemy);  
    return id;  
}

If you already remove dead actors inside `Tick`, keep that behavior (iterate a temp list and `actors.Remove(id)` after the loop). No Unity types here.

### **B) Net — NetEnemy view (replicates pos/hp to all clients)**

**File (new):** `Assets/Hellscape/Scripts/Net/NetEnemy.cs`

using UnityEngine;  
using Unity.Netcode;

namespace Hellscape.Net  
{  
    \[RequireComponent(typeof(NetworkObject), typeof(SpriteRenderer))\]  
    public sealed class NetEnemy : NetworkBehaviour  
    {  
        public readonly NetworkVariable\<Vector2\> netPos \=  
            new(writePerm: NetworkVariableWritePermission.Server);  
        public readonly NetworkVariable\<short\> netHp \=  
            new(writePerm: NetworkVariableWritePermission.Server);

        SpriteRenderer sr;  
        Color baseColor;

        void Awake()  
        {  
            sr \= GetComponent\<SpriteRenderer\>();  
            if (sr) baseColor \= sr.color;  
        }

        void Update()  
        {  
            // Everyone renders from replicated vars  
            transform.position \= netPos.Value;

            // Optional tiny feedback by HP  
            if (sr)  
            {  
                float t \= Mathf.Clamp01(1f \- (netHp.Value / 100f)); // assumes \~100 hp baseline  
                sr.color \= Color.Lerp(baseColor, Color.red, t \* 0.3f);  
            }

            // Auto-despawn visual if HP hits 0 (server should also clear its map)  
            if (IsSpawned && netHp.Value \<= 0 && \!IsServer)  
            {  
                // client waits for server despawn; no-op here  
            }  
        }  
    }  
}

Create a **`NetEnemy` prefab**:

* Components: `NetworkObject`, `SpriteRenderer`, `NetEnemy`.

* Add to **NetworkManager → NetworkPrefabs**.

---

### **C) App — SimGameServer (spawn NetEnemy with Domain actor, replicate state)**

**File:** `Assets/Hellscape/Scripts/App/SimGameServer.cs`

1. **Fields** (add if missing):

\[SerializeField\] Hellscape.Net.NetEnemy netEnemyPrefab;

readonly Dictionary\<int, Hellscape.Net.NetEnemy\> actorToNetEnemy \= new();

2. **Spawn a domain enemy AND its networked view**:

// Spawns Domain actor \+ Net view; maps actorId \<-\> view

void SpawnEnemyAt(UnityEngine.Vector2 worldPos)

{

    var dpos \= new Hellscape.Domain.Vector2(worldPos.x, worldPos.y);

    int actorId \= sim.SpawnEnemyAt(dpos);

    var view \= Instantiate(netEnemyPrefab, worldPos, Quaternion.identity);

    view.NetworkObject.Spawn(true);

    actorToNetEnemy\[actorId\] \= view;

    // Initialize net vars so clients see correct initial state

    if (sim.TryGetActorState(actorId, out var st))

    {

        view.netPos.Value \= new UnityEngine.Vector2(st.positionX, st.positionY);

        view.netHp.Value  \= st.hp;

    }

}

3. **Initial edge spawns on server start / first connect** (replace any domain-only call):

// Instead of sim.SpawnEnemiesAtEdges(...):

int initial \= 12;

for (int i \= 0; i \< initial; i++)

{

    var dpos \= sim.GetRandomEdgePositionForBridge(1.5f);

    SpawnEnemyAt(new UnityEngine.Vector2(dpos.x, dpos.y));

}

4. **Per-tick replication & cleanup** in `FixedUpdate()` **after** `sim.Tick(...)`:

// Push sim \-\> net vars and cull dead/missing

var toRemove \= new List\<int\>();

foreach (var kvp in actorToNetEnemy)

{

    int actorId \= kvp.Key;

    var view \= kvp.Value;

    if (view \== null || \!view.IsSpawned) { toRemove.Add(actorId); continue; }

    if (sim.TryGetActorState(actorId, out var st))

    {

        view.netPos.Value \= new UnityEngine.Vector2(st.positionX, st.positionY);

        view.netHp.Value  \= st.hp;

        if (st.hp \<= 0\)

        {

            // Server despawns the view; client receives it via NGO

            view.NetworkObject.Despawn(true);

            toRemove.Add(actorId);

        }

    }

    else

    {

        // Actor no longer exists in sim; clean up view

        view.NetworkObject.Despawn(true);

        toRemove.Add(actorId);

    }

}

foreach (var id in toRemove) actorToNetEnemy.Remove(id);

## **Inspector checklist**

* **SimGameServer**: assign `netEnemyPrefab`.

* **NetworkManager → NetworkPrefabs**: add the `NetEnemy` prefab.

* The playable scene has `NetworkManager`, `UnityTransport`, `RelayBootstrap`, `SimGameServer`, and your Player Prefab set.

---

## **Acceptance**

1. Host a session (Relay). You should see \~12 enemies spawn at the playfield edges.

2. Join from a second instance: both see the same enemies advancing.

3. (If you’ve already implemented shooting) killing an enemy removes it on both server and clients.

4. If you enabled the top-up spawner, new enemies trickle in until the cap.

---

## **Tests (EditMode, Domain)**

Add quick sanity tests (Domain only):

**File (new):** `Assets/Hellscape/Tests/EditMode/ServerSim_SpawnEdge_Tests.cs`

using NUnit.Framework;

using Hellscape.Domain;

public class ServerSim\_SpawnEdge\_Tests

{

    \[Test\]

    public void EdgeSpawn\_IsOnBoundsOrInside()

    {

        var sim \= new ServerSim(seed:1234);

        sim.Start();

        var p \= sim.GetRandomEdgePositionForBridge(1.5f);

        // crude bounds check against default 25x14 half-extents

        Assert.IsTrue(p.x \<= 25f && p.x \>= \-25f);

        Assert.IsTrue(p.y \<= 14f && p.y \>= \-14f);

        // and at least close to an edge (within \~1.6 of any side)

        bool nearX \= (25f \- System.Math.Abs(p.x)) \<= 1.6f;

        bool nearY \= (14f \- System.Math.Abs(p.y)) \<= 1.6f;

        Assert.IsTrue(nearX || nearY);

    }

}