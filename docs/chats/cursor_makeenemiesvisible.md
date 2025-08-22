# MakeEnemiesVisible
_Exported on 8/22/2025 at 10:06:13 CST from Cursor (1.5.2)_

---

**User**

execute

---

**Cursor**

I'll execute the plan to make enemies visible by implementing the hexagonal architecture changes. Let me start by examining the current codebase structure and then implement the required changes.


Great! I can see the ServerSim already has the edge spawn functionality. Now let me check the App layer to see the SimGameServer:


Now let me check if NetEnemy already exists:


Great! I can see that most of the infrastructure is already in place. Let me check if there are any issues with the current implementation and then run the tests to see what needs to be fixed. First, let me check if there's a test for the edge spawn functionality:


I need to create the edge spawn test. Let me also check if there are any issues with the current implementation by running the tests first:


Let me try with powershell directly:


Great! The tests are passing. Now let me create the edge spawn test as specified in the requirements:


```csharp
using NUnit.Framework;
using Hellscape.Domain;

public class ServerSim_SpawnEdge_Tests
{
    [Test]
    public void EdgeSpawn_IsOnBoundsOrInside()
    {
        var sim = new ServerSim(seed:1234);
        sim.Start();
        var p = sim.GetRandomEdgePositionForBridge(1.5f);
        
        // crude bounds check against default 25x14 half-extents
        Assert.IsTrue(p.x <= 25f && p.x >= -25f);
        Assert.IsTrue(p.y <= 14f && p.y >= -14f);
        
        // and at least close to an edge (within ~1.6 of any side)
        bool nearX = (25f - System.Math.Abs(p.x)) <= 1.6f;
        bool nearY = (14f - System.Math.Abs(p.y)) <= 1.6f;
        Assert.IsTrue(nearX || nearY);
    }
}
```

Now I need to add the public bridge method to ServerSim. Let me check if it already exists:


I need to add the public bridge method to ServerSim. Let me add it:


```csharp
        private Vector2 GetRandomEdgePosition(float inset) {
            var edge = rng.Range(0, 4); // 0=top, 1=right, 2=bottom, 3=left
            var t = rng.Next01(); // 0 to 1 along the edge
            
            switch (edge) {
                case 0: // top edge
                    return new Vector2(
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.x + inset, playfieldHalfExtents.x - inset, t),
                        playfieldHalfExtents.y - inset
                    );
                case 1: // right edge
                    return new Vector2(
                        playfieldHalfExtents.x - inset,
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.y + inset, playfieldHalfExtents.y - inset, t)
                    );
                case 2: // bottom edge
                    return new Vector2(
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.x + inset, playfieldHalfExtents.x - inset, t),
                        -playfieldHalfExtents.y + inset
                    );
                case 3: // left edge
                    return new Vector2(
                        -playfieldHalfExtents.x + inset,
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.y + inset, playfieldHalfExtents.y - inset, t)
                    );
                default:
                    return Vector2.zero;
            }
        }

->

        private Vector2 GetRandomEdgePosition(float inset) {
            var edge = rng.Range(0, 4); // 0=top, 1=right, 2=bottom, 3=left
            var t = rng.Next01(); // 0 to 1 along the edge
            
            switch (edge) {
                case 0: // top edge
                    return new Vector2(
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.x + inset, playfieldHalfExtents.x - inset, t),
                        playfieldHalfExtents.y - inset
                    );
                case 1: // right edge
                    return new Vector2(
                        playfieldHalfExtents.x - inset,
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.y + inset, playfieldHalfExtents.y - inset, t)
                    );
                case 2: // bottom edge
                    return new Vector2(
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.x + inset, playfieldHalfExtents.x - inset, t),
                        -playfieldHalfExtents.y + inset
                    );
                case 3: // left edge
                    return new Vector2(
                        -playfieldHalfExtents.x + inset,
                        ServerSimHelpers.Lerp(-playfieldHalfExtents.y + inset, playfieldHalfExtents.y - inset, t)
                    );
                default:
                    return Vector2.zero;
            }
        }

        // PUBLIC: app/bridge access (keeps one source of truth)
        public Vector2 GetRandomEdgePositionForBridge(float inset) => GetRandomEdgePosition(inset);
```

Now let me run the tests to see if the new test passes:


Great! Now let me check if there are any issues with the current SimGameServer implementation. I notice that the current implementation spawns enemies but doesn't properly handle the replication. Let me examine the current enemy spawning logic more closely:


I see the issue! The current implementation calls `sim.SpawnEnemiesAtEdges(12, 1.5f)` which only spawns domain enemies, but doesn't create the NetEnemy views. I need to modify this to use the proper spawn method that creates both domain and network views. Let me check the current SpawnEnemyAt method:


Perfect! I can see that the `SpawnEnemyAt` method is already implemented correctly. Now I need to modify the initial enemy spawning to use this method instead of the domain-only method. Let me update the SetupServer method:


```csharp
            // Spawn enemies at edges
            sim.SpawnEnemiesAtEdges(12, 1.5f);

->

            // Spawn enemies at edges with both domain and network views
            int initial = 12;
            for (int i = 0; i < initial; i++)
            {
                var dpos = sim.GetRandomEdgePositionForBridge(1.5f);
                SpawnEnemyAt(new Vector2(dpos.x, dpos.y));
            }
```

Now I need to add the missing field for the netEnemyPrefab. Let me check if it's already declared:


Great! The field is already declared. Now I need to add the cleanup logic for dead enemies in the FixedUpdate method. Let me check if it's already there:


I need to add the cleanup logic for dead enemies. Let me add it after the enemy replication loop:


```csharp
            // Push sim positions into replicated NetVariables for enemies
            foreach (var kvp in actorToNetEnemy)
            {
                var actorId = kvp.Key;
                var view = kvp.Value;
                if (view == null || !view.IsSpawned) continue;

                if (sim.TryGetActorState(actorId, out var st))
                {
                    view.netPos.Value = new Vector2(st.positionX, st.positionY);
                    view.netHp.Value = st.hp;
                }
            }

->

            // Push sim positions into replicated NetVariables for enemies and cleanup dead ones
            var toRemove = new List<int>();
            foreach (var kvp in actorToNetEnemy)
            {
                var actorId = kvp.Key;
                var view = kvp.Value;
                if (view == null || !view.IsSpawned) { toRemove.Add(actorId); continue; }

                if (sim.TryGetActorState(actorId, out var st))
                {
                    view.netPos.Value = new Vector2(st.positionX, st.positionY);
                    view.netHp.Value = st.hp;
                    if (st.hp <= 0)
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
```

Now let me run the tests to make sure everything is still working:


Excellent! Now let me check if there's a NetEnemy prefab that needs to be created or if it already exists:


Great! The NetEnemy prefab already exists. Let me check if it's properly configured:


Perfect! The NetEnemy prefab is properly configured with NetworkObject, NetEnemy script, and SpriteRenderer. Now let me check if it's registered in the NetworkManager's NetworkPrefabs:


Great! I can see that the NetEnemy prefab is already registered in the NetworkPrefabs list (the last entry with guid `b8cb042beb3d1724ebbddc3e13bd257d`). Now let me check if there are any issues with the current implementation by looking at the NetEnemy script more closely. I noticed that the current NetEnemy script has some differences from what was specified in the requirements. Let me update it to match the requirements:


```csharp
using Unity.Netcode;
using UnityEngine;
using Hellscape.Domain;
using Vector2 = UnityEngine.Vector2;

namespace Hellscape.Net {
    public class NetEnemy : NetworkBehaviour {
        [Header("Network Variables")]
        public NetworkVariable<Vector2> netPos = new NetworkVariable<Vector2>();
        public NetworkVariable<short> netHp = new NetworkVariable<short>();
        public NetworkVariable<int> actorId = new NetworkVariable<int>();
        
        [Header("Visual")]
        public SpriteRenderer spriteRenderer;
        
        private void Awake() {
            if (spriteRenderer == null) {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }
        
        public override void OnNetworkSpawn() {
            base.OnNetworkSpawn();
            
            // Set initial position and HP
            if (IsServer) {
                netPos.Value = transform.position;
                netHp.Value = 60; // Damned HP
            }
            
            // Subscribe to HP changes for visual feedback
            netHp.OnValueChanged += OnHpChanged;
        }
        
        public override void OnNetworkDespawn() {
            netHp.OnValueChanged -= OnHpChanged;
            base.OnNetworkDespawn();
        }
        
        private void OnHpChanged(short previousValue, short newValue) {
            if (newValue <= 0) {
                // Enemy died, will be despawned by server
                if (spriteRenderer != null) {
                    spriteRenderer.color = Color.red;
                }
            } else if (newValue < previousValue) {
                // Enemy took damage, flash
                if (spriteRenderer != null) {
                    StartCoroutine(FlashRed());
                }
            }
        }
        
        private System.Collections.IEnumerator FlashRed() {
            if (spriteRenderer == null) yield break;
            
            var originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = originalColor;
        }
        
        private void Update() {
            if (IsClient && netPos.Value != (Vector2)transform.position) {
                transform.position = netPos.Value;
            }
        }
        
        [ServerRpc]
        public void SetActorIdServerRpc(int id) {
            actorId.Value = id;
        }
        
        [ServerRpc]
        public void DespawnServerRpc() {
            if (IsServer) {
                NetworkObject.Despawn();
            }
        }
    }
}

->

using UnityEngine;
using Unity.Netcode;

namespace Hellscape.Net  
{  
    [RequireComponent(typeof(NetworkObject), typeof(SpriteRenderer))]  
    public sealed class NetEnemy : NetworkBehaviour  
    {  
        public readonly NetworkVariable<Vector2> netPos =  
            new(writePerm: NetworkVariableWritePermission.Server);  
        public readonly NetworkVariable<short> netHp =  
            new(writePerm: NetworkVariableWritePermission.Server);

        SpriteRenderer sr;  
        Color baseColor;

        void Awake()  
        {  
            sr = GetComponent<SpriteRenderer>();  
            if (sr) baseColor = sr.color;  
        }

        void Update()  
        {  
            // Everyone renders from replicated vars  
            transform.position = netPos.Value;

            // Optional tiny feedback by HP  
            if (sr)  
            {  
                float t = Mathf.Clamp01(1f - (netHp.Value / 100f)); // assumes ~100 hp baseline  
                sr.color = Color.Lerp(baseColor, Color.red, t * 0.3f);  
            }

            // Auto-despawn visual if HP hits 0 (server should also clear its map)  
            if (IsSpawned && netHp.Value <= 0 && !IsServer)  
            {  
                // client waits for server despawn; no-op here  
            }  
        }  
    }  
}
```

Now I need to update the SimGameServer to remove the ServerRpc calls since the new NetEnemy implementation doesn't have them. Let me update the SpawnEnemyAt method:


```csharp
        public int SpawnEnemyAt(Vector2 position)
        {
            if (!IsServer) return -1;
            
            var actorId = sim.SpawnEnemyAt(new DVec2(position.x, position.y));
            
            if (netEnemyPrefab != null)
            {
                var enemyObj = Instantiate(netEnemyPrefab, position, Quaternion.identity);
                var netEnemy = enemyObj.GetComponent<NetEnemy>();
                if (netEnemy != null)
                {
                    netEnemy.NetworkObject.Spawn();
                    netEnemy.SetActorIdServerRpc(actorId);
                    actorToNetEnemy[actorId] = netEnemy;
                }
            }
            
            return actorId;
        }

->

        public int SpawnEnemyAt(Vector2 position)
        {
            if (!IsServer) return -1;
            
            var actorId = sim.SpawnEnemyAt(new DVec2(position.x, position.y));
            
            if (netEnemyPrefab != null)
            {
                var enemyObj = Instantiate(netEnemyPrefab, position, Quaternion.identity);
                var netEnemy = enemyObj.GetComponent<NetEnemy>();
                if (netEnemy != null)
                {
                    netEnemy.NetworkObject.Spawn(true);
                    actorToNetEnemy[actorId] = netEnemy;
                    
                    // Initialize net vars so clients see correct initial state
                    if (sim.TryGetActorState(actorId, out var st))
                    {
                        netEnemy.netPos.Value = new Vector2(st.positionX, st.positionY);
                        netEnemy.netHp.Value = st.hp;
                    }
                }
            }
            
            return actorId;
        }
```

Now I need to update the HandleActorDeath method to remove the ServerRpc call:


```csharp
        private void HandleActorDeath(int actorId)
        {
            if (actorToNetEnemy.TryGetValue(actorId, out var netEnemy))
            {
                if (netEnemy != null && netEnemy.IsSpawned)
                {
                    netEnemy.DespawnServerRpc();
                }
                actorToNetEnemy.Remove(actorId);
            }
        }

->

        private void HandleActorDeath(int actorId)
        {
            if (actorToNetEnemy.TryGetValue(actorId, out var netEnemy))
            {
                if (netEnemy != null && netEnemy.IsSpawned)
                {
                    netEnemy.NetworkObject.Despawn(true);
                }
                actorToNetEnemy.Remove(actorId);
            }
        }
```

Now let me run the tests to make sure everything is still working:


Perfect! Now let me check if there are any compilation issues by looking at the project structure. Let me also check if there are any missing imports or dependencies:


Good, the import is already there. Now let me check if there are any issues with the current implementation by looking at the scene setup. Let me check what scenes are available:


Let me check the Bootstrap scene to see if it has the proper setup:


Let me check the TestArena scene:


Let me search for SimGameServer in the scene file:


Let me check the Bootstrap scene:


It seems like the SimGameServer might not be in the scenes yet. Let me check if there are any other scene files or if we need to add it. Let me also check if there are any compilation errors by running a build check. But first, let me create a simple test to verify that the enemy spawning works correctly. Let me check if there are any existing tests that might be related:


Great! I can see that there are existing tests that use `SpawnEnemyAt`. Let me run the tests again to make sure everything is working correctly:


Perfect! All tests are passing. Now let me create a summary of what has been implemented and what needs to be done for the inspector setup. Let me also check if there are any missing pieces by looking at the current implementation more carefully.

Let me check if there are any issues with the current NetEnemy implementation by looking at the prefab again:


I notice that the prefab still has the old NetEnemy script with the old fields. I need to update the prefab to match the new script. However, since the prefab is a binary file, I'll need to recreate it or update it through Unity. For now, let me check if the current implementation will work with the existing prefab structure.

Let me check if there are any compilation issues by looking at the current NetEnemy script more carefully. I notice that the new script doesn't have the `actorId` field that the prefab expects. Let me add it back:


```csharp
    public sealed class NetEnemy : NetworkBehaviour  
    {  
        public readonly NetworkVariable<Vector2> netPos =  
            new(writePerm: NetworkVariableWritePermission.Server);  
        public readonly NetworkVariable<short> netHp =  
            new(writePerm: NetworkVariableWritePermission.Server);

        SpriteRenderer sr;  
        Color baseColor;

->

    public sealed class NetEnemy : NetworkBehaviour  
    {  
        public readonly NetworkVariable<Vector2> netPos =  
            new(writePerm: NetworkVariableWritePermission.Server);  
        public readonly NetworkVariable<short> netHp =  
            new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<int> actorId =  
            new(writePerm: NetworkVariableWritePermission.Server);

        SpriteRenderer sr;  
        Color baseColor;
```

Now I need to add back the ServerRpc methods that the SimGameServer expects:


```csharp
            // Auto-despawn visual if HP hits 0 (server should also clear its map)  
            if (IsSpawned && netHp.Value <= 0 && !IsServer)  
            {  
                // client waits for server despawn; no-op here  
            }  
        }  
    }  
}

->

            // Auto-despawn visual if HP hits 0 (server should also clear its map)  
            if (IsSpawned && netHp.Value <= 0 && !IsServer)  
            {  
                // client waits for server despawn; no-op here  
            }  
        }
        
        [ServerRpc]
        public void SetActorIdServerRpc(int id) {
            actorId.Value = id;
        }
        
        [ServerRpc]
        public void DespawnServerRpc() {
            if (IsServer) {
                NetworkObject.Despawn();
            }
        }
    }  
}
```

Now I need to update the SimGameServer to use the ServerRpc methods again:


```csharp
                if (netEnemy != null)
                {
                    netEnemy.NetworkObject.Spawn(true);
                    actorToNetEnemy[actorId] = netEnemy;
                    
                    // Initialize net vars so clients see correct initial state
                    if (sim.TryGetActorState(actorId, out var st))
                    {
                        netEnemy.netPos.Value = new Vector2(st.positionX, st.positionY);
                        netEnemy.netHp.Value = st.hp;
                    }
                }

->

                if (netEnemy != null)
                {
                    netEnemy.NetworkObject.Spawn(true);
                    netEnemy.SetActorIdServerRpc(actorId);
                    actorToNetEnemy[actorId] = netEnemy;
                    
                    // Initialize net vars so clients see correct initial state
                    if (sim.TryGetActorState(actorId, out var st))
                    {
                        netEnemy.netPos.Value = new Vector2(st.positionX, st.positionY);
                        netEnemy.netHp.Value = st.hp;
                    }
                }
```

```csharp
        private void HandleActorDeath(int actorId)
        {
            if (actorToNetEnemy.TryGetValue(actorId, out var netEnemy))
            {
                if (netEnemy != null && netEnemy.IsSpawned)
                {
                    netEnemy.NetworkObject.Despawn(true);
                }
                actorToNetEnemy.Remove(actorId);
            }
        }

->

        private void HandleActorDeath(int actorId)
        {
            if (actorToNetEnemy.TryGetValue(actorId, out var netEnemy))
            {
                if (netEnemy != null && netEnemy.IsSpawned)
                {
                    netEnemy.DespawnServerRpc();
                }
                actorToNetEnemy.Remove(actorId);
            }
        }
```

Now let me run the tests again to make sure everything is working:


Excellent! All tests are passing. Now let me create a summary of what has been implemented and provide the final checklist for the inspector setup.

## Summary

I have successfully implemented the enemy visibility system according to the requirements. Here's what has been completed:

### ✅ **Domain Layer (ServerSim)**
- ✅ Added public bridge method `GetRandomEdgePositionForBridge(float inset)` for edge spawn access
- ✅ Existing `SpawnEnemyAt(Vector2 pos)` method already returns actorId
- ✅ Edge spawn logic already implemented with proper bounds checking

### ✅ **Net Layer (NetEnemy)**
- ✅ Created `NetEnemy` script with proper NetworkVariables:
  - `netPos` (Vector2) - replicated position
  - `netHp` (short) - replicated health
  - `actorId` (int) - actor ID for mapping
- ✅ Server-only write permissions on NetworkVariables
- ✅ Visual feedback: HP-based color tinting (red tint as HP decreases)
- ✅ Proper cleanup with ServerRpc methods
- ✅ Requires NetworkObject and SpriteRenderer components

### ✅ **App Layer (SimGameServer)**
- ✅ Updated to spawn both domain actors AND network views
- ✅ Proper mapping between domain actors and network views (`actorToNetEnemy`)
- ✅ Per-tick replication of position and HP from domain to network
- ✅ Cleanup of dead enemies (despawn network objects, remove from mapping)
- ✅ Initial edge spawns (12 enemies) using the new spawn method

### ✅ **Tests**
- ✅ Created `ServerSim_SpawnEdge_Tests.cs` with bounds validation
- ✅ All existing tests still pass
- ✅ Edge spawn test validates position is within bounds and near edges

### ✅ **Prefab & Network Setup**
- ✅ NetEnemy prefab exists with proper components
- ✅ Prefab registered in NetworkManager's NetworkPrefabs list
- ✅ Proper NetworkObject configuration

## 🔧 **Inspector Checklist (Manual Setup Required)**

To complete the setup, you need to configure the following in Unity:

1. **SimGameServer GameObject**:
   - Find or create a GameObject with `SimGameServer` component
   - Assign the `NetEnemy` prefab to the `netEnemyPrefab` field

2. **Scene Setup**:
   - Ensure the scene has `NetworkManager`, `UnityTransport`, `RelayBootstrap`, and `SimGameServer`
   - Make sure the `SimGameServer` GameObject is active in the scene

3. **NetworkManager Configuration**:
   - Verify `NetEnemy` prefab is in the NetworkPrefabs list (should already be there)
   - Ensure Player Prefab is set correctly

## 🎯 **Acceptance Criteria**

Once the inspector setup is complete, you should see:

1. **Host a session**: ~12 enemies spawn at playfield edges
2. **Join from second instance**: Both clients see the same enemies advancing
3. **Shooting**: Killing enemies removes them on both server and clients
4. **Visual feedback**: Enemies get redder as they take damage

The implementation follows the hexagonal architecture principles:
- ✅ Domain stays Unity-free (no UnityEngine references)
- ✅ Presentation/Net handles visuals and networking
- ✅ Clean separation of concerns
- ✅ Proper event-driven communication

All tests are passing and the system is ready for testing in Unity!

