You are Cursor working on the Hellscape repo with `.cursorrules` loaded.
**Do NOT** run `dotnet test`. For tests, use Unity’s CLI:
- Windows: powershell -ExecutionPolicy Bypass -File docs\tools\unity-test.ps1

## Feature
Fix hitscan so shots register from any direction (not only when player is left of enemy) and add basic shooting VFX:
- Robust segment–circle (capsule) hit test in Domain (direction-agnostic).
- Server-authoritative firing uses proper aim (owner sends aim → server).
- Tracer + muzzle flash VFX, broadcast via ClientRpc (cosmetic only).

Keep hex boundaries: math in **Domain**, VFX in **Net/Presentation**, server logic in **App** bridge.

---

## A) Domain — Robust hitscan math + shot events

**1) Add capsule/segment intersection utils**

**File (new):** `Assets/Hellscape/Scripts/Domain/Combat/Hitscan.cs`
```csharp
namespace Hellscape.Domain.Combat
{
    // Lightweight vector helpers for Domain.Vector2
    internal static class V2
    {
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 Sub(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 Add(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static Vector2 Mul(Vector2 a, float k) => new Vector2(a.x * k, a.y * k);
        public static float LenSq(Vector2 a) => Dot(a, a);
    }

    public static class Hitscan
    {
        // Returns true if circle (center C, radius r) intersects segment AB.
        // Out t is clamped to [0,1] and is the closest point on AB to C.
        public static bool SegmentCircle(Vector2 a, Vector2 b, Vector2 c, float r, out float t)
        {
            var ab = V2.Sub(b, a);
            var ac = V2.Sub(c, a);
            var abLenSq = V2.LenSq(ab);
            if (abLenSq <= 1e-8f) { t = 0f; return V2.LenSq(ac) <= r * r; }

            t = V2.Dot(ac, ab) / abLenSq; // projection parameter (can be <0 or >1)
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;

            var closest = V2.Add(a, V2.Mul(ab, t));
            var distSq = V2.LenSq(V2.Sub(c, closest));
            return distSq <= r * r;
        }
    }

    // Domain-level fired shot event. Read by App after Tick and cleared.
    public struct ShotEvent
    {
        public Vector2 start, end;
        public bool hit; // optional
        public ShotEvent(Vector2 s, Vector2 e, bool h) { start = s; end = e; hit = h; }
    }
}
**2\) Emit shot events from the sim and use the new math**

**File:** `Assets/Hellscape/Scripts/Domain/ServerSim.cs`

* Add a per-tick queue:

using Hellscape.Domain.Combat;  
// ...  
private readonly System.Collections.Generic.List\<ShotEvent\> \_shotEvents \= new();  
// Expose a safe consumer:  
public System.Collections.Generic.IReadOnlyList\<ShotEvent\> ConsumeShotEvents()  
{  
    var list \= \_shotEvents.ToArray();  
    \_shotEvents.Clear();  
    return list;  
}

* In your Actor firing logic (when `AttackButtonBit` and cooldown ≤ 0), compute:

  * Ray start \= actor position

  * Ray end \= start \+ normalized(aim) \* weapon.Range

  * For each enemy, test `Hitscan.SegmentCircle(start, end, enemy.pos, weapon.HitRadius, out t)`

  * Track the **closest** hit by smallest `t` in \[0,1\]

  * Apply damage to that enemy (existing DamageResolver), set `hit=true` if any

  * Enqueue `_shotEvents.Add(new ShotEvent(start, end, hit));`

Make no Unity references here.

---

## **B) Net/App — Authoritative aim \+ broadcast VFX**

**1\) Send aim from owner to server**

**File:** `Assets/Hellscape/Scripts/Net/NetPlayer.cs`

* Add owner-side aim world-direction and include in ServerRpc:

\[SerializeField\] float rpcRate \= 20f;  
private Vector2 \_moveInput;  
private Vector2 \_aimWorldDir \= Vector2.right; // default  
private Camera \_cam;

public override void OnNetworkSpawn()  
{  
    if (IsOwner)  
    {  
        \_cam \= Camera.main; // top-down main camera  
        // input setup as before...  
    }  
}

// Owner input callback for "Look" should give screen/touch position.  
// Convert to world and build direction to player.  
public void OnLook(InputAction.CallbackContext ctx)  
{  
    var screenPos \= ctx.ReadValue\<UnityEngine.Vector2\>();  
    if (\_cam)  
    {  
        var wp \= \_cam.ScreenToWorldPoint(new UnityEngine.Vector3(screenPos.x, screenPos.y, 0f));  
        var dir \= ((UnityEngine.Vector2)wp \- (UnityEngine.Vector2)transform.position);  
        if (dir.sqrMagnitude \> 1e-6f) \_aimWorldDir \= dir.normalized;  
    }  
}

void Update()  
{  
    transform.position \= netPos.Value; // as before  
    if (\!IsOwner) return;

    \_rpcAccum \+= Time.deltaTime;  
    if (\_rpcAccum \>= (1f / Mathf.Max(1f, rpcRate)))  
    {  
        var mv \= \_moveInput.sqrMagnitude \> 1f ? \_moveInput.normalized : \_moveInput;  
        SubmitInputServerRpc(mv, \_aimWorldDir);  
        \_rpcAccum \= 0f;  
    }  
}

\[ServerRpc(RequireOwnership \= true)\]  
void SubmitInputServerRpc(Vector2 move, Vector2 aimDir)  
{  
    NetSim.Bridge?.SubmitInputFrom(OwnerClientId, move, aimDir);  
}

**2\) Pass aim through the server bridge to the sim**

**File:** `Assets/Hellscape/Scripts/App/SimGameServer.cs`

* Update bridge signature and InputCommand creation:

public void SubmitInputFrom(ulong clientId, UnityEngine.Vector2 move, UnityEngine.Vector2 aimDir, byte buttons \= 0\)  
{  
    if (\!IsServer) return;  
    if (\!clientToActor.TryGetValue(clientId, out var actorId)) return;

    // Normalize aim; fallback to right if zero  
    var aim \= aimDir.sqrMagnitude \> 1e-6f ? aimDir.normalized : new UnityEngine.Vector2(1, 0);

    var cmd \= new InputCommand(  
        tick: 0,  
        moveX: move.x, moveY: move.y,  
        aimX: aim.x,  aimY: aim.y,  
        buttons: buttons  
    );  
    sim.ApplyForActor(actorId, cmd);  
}

*(Adjust your existing signatures; NetPlayer → Bridge → ServerSim.ApplyForActor already exist.)*

**3\) VFX broadcast on shot**

Add a tiny client-side VFX spawner and a ClientRpc hooked from the server:

**File (new):** `Assets/Hellscape/Scripts/Net/ShotVfx.cs`

using UnityEngine;

namespace Hellscape.Net  
{  
    public sealed class ShotVfx : MonoBehaviour  
    {  
        \[SerializeField\] GameObject tracerPrefab;   // thin line sprite or LineRenderer  
        \[SerializeField\] GameObject muzzleFlashPrefab; // small sprite

        public void PlayTracer(Vector2 start, Vector2 end)  
        {  
            if (tracerPrefab \== null) return;  
            var go \= Instantiate(tracerPrefab);  
            var dir \= end \- start;  
            var len \= dir.magnitude;  
            if (len \< 0.01f) { Destroy(go, 0.03f); return; }

            go.transform.position \= start;  
            go.transform.right \= dir.normalized;  
            go.transform.localScale \= new Vector3(len, go.transform.localScale.y, 1f);  
            Destroy(go, 0.06f); // very brief  
        }

        public void PlayMuzzle(Vector2 at)  
        {  
            if (muzzleFlashPrefab \== null) return;  
            var go \= Instantiate(muzzleFlashPrefab, at, Quaternion.identity);  
            Destroy(go, 0.05f);  
        }  
    }  
}

**File:** `Assets/Hellscape/Scripts/App/SimGameServer.cs`

* Add a `ShotVfx` reference and a ClientRpc:

\[SerializeField\] Hellscape.Net.ShotVfx shotVfx; // assign in scene

\[ClientRpc\]  
void ShotFxClientRpc(UnityEngine.Vector2 start, UnityEngine.Vector2 end)  
{  
    if (shotVfx \!= null) shotVfx.PlayTracer(start, end);  
}

After `sim.Tick(...)`, consume shot events and broadcast:

// after sim.Tick in FixedUpdate:  
var shots \= sim.ConsumeShotEvents();  
if (shots \!= null)  
{  
    foreach (var s in shots)  
    {  
        var sStart \= new UnityEngine.Vector2(s.start.x, s.start.y);  
        var sEnd   \= new UnityEngine.Vector2(s.end.x, s.end.y);  
        ShotFxClientRpc(sStart, sEnd);  
    }  
}

## **C) Minimal prefabs / inspector**

* **Tracer prefab:** a small horizontal sprite (1×1 white) or `LineRenderer` with width \~0.05; material default; pivot at left so scaling X stretches along direction.

* **Muzzle flash prefab:** tiny sprite with additive material; auto-destroy in `ShotVfx`.

* **Scene:** Add a `ShotVfx` GameObject with the `ShotVfx` component; assign both prefabs.

* **SimGameServer:** drag the `ShotVfx` instance into its serialized field.

---

## **D) Tests (EditMode, Domain)**

**File (new):** `Assets/Hellscape/Tests/EditMode/Hitscan_Tests.cs`

using NUnit.Framework;  
using Hellscape.Domain;  
using Hellscape.Domain.Combat;

public class Hitscan\_Tests  
{  
    \[Test\] public void Hits\_Rightward\_Line()  
    {  
        var a \= new Vector2(0,0);  
        var b \= new Vector2(10,0);  
        var c \= new Vector2(5,0);  
        Assert.IsTrue(Hitscan.SegmentCircle(a,b,c,0.5f, out var t));  
        Assert.That(t, Is.InRange(0.0f, 1.0f));  
    }

    \[Test\] public void Hits\_Leftward\_Line()  
    {  
        var a \= new Vector2(10,0);  
        var b \= new Vector2(0,0);  // reversed direction  
        var c \= new Vector2(5,0);  
        Assert.IsTrue(Hitscan.SegmentCircle(a,b,c,0.5f, out var t));  
        Assert.That(t, Is.InRange(0.0f, 1.0f));  
    }

    \[Test\] public void Misses\_When\_Outside\_Radius()  
    {  
        var a \= new Vector2(0,0);  
        var b \= new Vector2(10,0);  
        var c \= new Vector2(5,1.0f);  
        Assert.IsFalse(Hitscan.SegmentCircle(a,b,c,0.4f, out var \_));  
    }  
}

## **Acceptance**

* Host \+ client: aiming in any direction hits when line crosses an enemy, regardless of player/enemy relative left/right.

* Tracer streaks show for all clients briefly; muzzle flash is visible at the start point (optional).

* No gameplay relies on VFX; shots still server-authoritative.

* Tests pass for both left-to-right and right-to-left cases.