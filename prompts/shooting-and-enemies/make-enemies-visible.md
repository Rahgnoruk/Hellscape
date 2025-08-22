You are Cursor working on the Hellscape repo with `.cursorrules` loaded.
**Do NOT** run `dotnet test`. For tests, use Unity’s CLI script:
`pwsh docs/tools/unity-test.ps1 -Platform EditMode`

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
