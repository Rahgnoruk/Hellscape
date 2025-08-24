using Hellscape.Domain;
using Hellscape.Net; // for NetPlayer
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using DVec2 = Hellscape.Domain.Vector2; // domain vector
using Vector2 = UnityEngine.Vector2; // Unity vector
namespace Hellscape.App
{
    public sealed class SimGameServer : NetworkBehaviour, INetSimBridge
    {
        public static SimGameServer Instance { get; private set; }

        [SerializeField] int seed = 42;
        [SerializeField] GameObject netEnemyPrefab;
        [SerializeField] ShotVfx shotVfx; // assign in scene

        private ServerSim sim;
        private readonly Dictionary<ulong, int> clientToActor = new();     // NGO client → Domain actor
        private readonly Dictionary<int, NetPlayer> actorToNetPlayer = new(); // Domain actor → Net view
        private readonly Dictionary<int, NetEnemy> actorToNetEnemy = new(); // Domain actor → Net enemy view
        private readonly Dictionary<int, InventoryState> _playerInventories = new(); // Domain actor → Inventory
        private float spawnTimer;
        
        // NetworkVariables for scoring and game state
        public readonly NetworkVariable<int> netTeamScore = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<bool> netGameOver = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<float> netReviveSeconds = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<int> netDeadAwaiting = new(writePerm: NetworkVariableWritePermission.Server);
        
        // Spawning difficulty ramp
        [SerializeField] float baseSpawnInterval = 1.0f;
        [SerializeField] float minSpawnInterval = 0.1f;
        [SerializeField] int baseEnemyCap = 30;
        [SerializeField] int maxEnemyCap = 60;
        [SerializeField] float rampSeconds = 360f; // reach full difficulty by 6 minutes
        
        private float elapsed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            if (NetworkManager && NetworkManager.IsServer) SetupServer();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;
            SetupServer();
            
        }
        void SetupServer()
        {
            if (NetSim.Bridge != null && NetSim.Bridge != this) return;
            // Install bridge so NetPlayer can reach us without referencing App
            NetSim.Bridge = this;

            // Create authoritative simulation
            if (sim == null)
            {
                sim = new ServerSim(seed);
                sim.Start();
            }

            // Hook connect/disconnect to clean mappings (optional; NetPlayer covers registration)
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            // Also register the host player if already connected
            foreach (var kvp in NetworkManager.ConnectedClients)
            {
                OnClientConnected(kvp.Key);
            }
            
            // Spawn enemies at edges with both domain and network views
            int initial = 12;
            for (int i = 0; i < initial; i++)
            {
                var dpos = sim.GetRandomEdgePositionForBridge(1.5f);
                SpawnEnemyAt(new Vector2(dpos.x, dpos.y));
            }
        }
        public override void OnDestroy()
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            // Uninstall bridge if we’re the one who set it
            if (NetSim.Bridge == this) NetSim.Bridge = null;

            if (Instance == this) Instance = null;
            base.OnDestroy();
        }
        void OnClientConnected(ulong clientId)
        {
            if (!IsServer) return;

            // PlayerObject is usually ready here; if not, wait a frame
            var po = NetworkManager.ConnectedClients[clientId].PlayerObject;
            if (po == null) { StartCoroutine(WaitAndRegister(clientId)); return; }

            var p = po.GetComponent<NetPlayer>();
            if (p != null) RegisterNetPlayerServer(p);
        }
        IEnumerator WaitAndRegister(ulong clientId)
        {
            var t = 0f;
            while (t < 1f)
            { // wait up to 1 second
                if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var cc)) yield break;
                var po = cc.PlayerObject;
                if (po != null)
                {
                    var p = po.GetComponent<NetPlayer>();
                    if (p != null) { RegisterNetPlayerServer(p); yield break; }
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        public void RegisterNetPlayerServer(NetPlayer netPlayer)
        {
            // Called from NetPlayer.OnNetworkSpawn on the SERVER side
            var cid = netPlayer.OwnerClientId;
            if (!clientToActor.TryGetValue(cid, out var actorId))
            {
                // Choose a simple spawn near outskirts
                var spawn = new DVec2(-10, 6);

                actorId = sim.SpawnPlayerAt(spawn);
                clientToActor[cid] = actorId;
            }
            actorToNetPlayer[actorId] = netPlayer;
        }

        // Inventory methods for INetSimBridge
        public int RegisterPlayerWithInventory(Vector2 spawn)
        {
            // Convert Unity Vector2 to Domain Vector2
            var domainPos = new DVec2(spawn.x, spawn.y);
            
            // Register with the sim and get actor ID
            var actorId = sim.SpawnPlayerAt(domainPos);
            
            // Initialize inventory for this player
            _playerInventories[actorId] = InventoryState.NewWithBase();
            
            return actorId;
        }

        public InventoryState GetInventory(int actorId)
        {
            if (_playerInventories.TryGetValue(actorId, out var inventory))
            {
                return inventory;
            }
            
            // Return default inventory if not found
            return InventoryState.NewWithBase();
        }

        public InventoryState SetActiveSlot(int actorId, int index)
        {
            if (_playerInventories.TryGetValue(actorId, out var inventory))
            {
                var newInventory = InventoryLogic.SetActive(inventory, index);
                _playerInventories[actorId] = newInventory;
                return newInventory;
            }
            
            return InventoryState.NewWithBase();
        }

        public InventoryState ApplyPickup(int actorId, PickupData loot, out bool dropped, out PickupData droppedPickup)
        {
            if (_playerInventories.TryGetValue(actorId, out var inventory))
            {
                var newInventory = InventoryLogic.ApplyPickup(inventory, loot, out dropped, out droppedPickup);
                _playerInventories[actorId] = newInventory;
                return newInventory;
            }
            
            dropped = false;
            droppedPickup = default;
            return InventoryState.NewWithBase();
        }

        public ConsumeResult TryConsumeAmmo(int actorId)
        {
            if (_playerInventories.TryGetValue(actorId, out var inventory))
            {
                var result = InventoryLogic.TryConsume(inventory);
                if (result.fired)
                {
                    _playerInventories[actorId] = result.next;
                }
                return result;
            }
            
            return new ConsumeResult { fired = false, next = InventoryState.NewWithBase() };
        }
        void OnClientDisconnected(ulong clientId)
        {
            if (clientToActor.TryGetValue(clientId, out var actorId))
            {
                clientToActor.Remove(clientId);
                actorToNetPlayer.Remove(actorId);
                _playerInventories.Remove(actorId);
                sim.RemovePlayerActor(actorId);
            }
        }

        public void SubmitInputFrom(ulong clientId, Vector2 move, byte buttons = 0)
        {
            // Redirect to the new signature with zero aim for backward compatibility
            SubmitInputFrom(clientId, move, Vector2.zero, buttons);
        }

        void FixedUpdate()
        {
            if (!IsServer || sim == null) return;

            // Tick authoritative sim
            sim.Tick(Time.fixedDeltaTime);

            // Process combat events
            while (sim.TryDequeueEvent(out var domainEvent))
            {
                switch (domainEvent.kind)
                {
                    case DomainEvent.Kind.HitLanded:
                        Debug.Log($"Hit landed: {domainEvent.attackerId} -> {domainEvent.targetId} ({domainEvent.damage} damage)");
                        break;
                    case DomainEvent.Kind.ActorDied:
                        HandleActorDeath(domainEvent.targetId);
                        break;
                }
            }

            // Push sim positions into replicated NetVariables for players
            foreach (var kvp in actorToNetPlayer)
            {
                var actorId = kvp.Key;
                var view = kvp.Value;
                if (view == null || !view.IsSpawned) continue;

                if (sim.TryGetActorState(actorId, out var st))
                {
                    view.netPos.Value = new Vector2(st.positionX, st.positionY);
                    view.netHp.Value = st.hp;
                    view.netAlive.Value = st.alive;
                }
            }
            
            // Process shot events and broadcast VFX
            var shots = sim.ConsumeShotEvents();
            if (shots != null)
            {
                foreach (var shot in shots)
                {
                    var sStart = new Vector2(shot.start.x, shot.start.y);
                    var sEnd = new Vector2(shot.end.x, shot.end.y);
                    ShotFxClientRpc(sStart, sEnd);
                }
            }
            
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
            
            // 1) Replicate meta
            netTeamScore.Value = sim.GetTeamScore();
            netReviveSeconds.Value = sim.GetReviveSecondsRemaining();
            netDeadAwaiting.Value = sim.GetDeadPlayerCount();
            
            // 2) Game over check (all players dead)
            if (!sim.AreThereAlivePlayers())
            {
                netGameOver.Value = true;
            }
            
            // 3) Ramped spawner (halt on game over)
            elapsed += Time.fixedDeltaTime;
            if (!netGameOver.Value)
            {
                float t = Mathf.Clamp01(elapsed / Mathf.Max(1f, rampSeconds));
                float spawnInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, t);
                int cap = Mathf.RoundToInt(Mathf.Lerp(baseEnemyCap, maxEnemyCap, t));
                
                spawnTimer += Time.fixedDeltaTime;
                if (spawnTimer >= spawnInterval && CurrentEnemyCount() < cap)
                {
                    spawnTimer = 0f;
                    var dpos = sim.GetRandomEdgePositionForBridge(1.2f);
                    SpawnEnemyAt(new Vector2(dpos.x, dpos.y));
                }
            }
        }
        
        public void SubmitInputFrom(ulong clientId, Vector2 move, Vector2 aim, byte buttons = 0)
        {
            if (!IsServer) return;
            if (!clientToActor.TryGetValue(clientId, out var actorId)) return;

            var cmd = new InputCommand(
              tick: 0, // not used by sim right now; sim consumes "latest"
              moveX: move.x, moveY: move.y,
              aimX: aim.x, aimY: aim.y,
              buttons: buttons
            );
            sim.ApplyForActor(actorId, cmd);
        }
        
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
                    netEnemy.SetActorIdServerRpc(actorId);
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
        
        [ClientRpc]
        void ShotFxClientRpc(Vector2 start, Vector2 end)
        {
            if (shotVfx != null) shotVfx.PlayTracer(start, end);
        }
        
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
        
        private int CurrentEnemyCount()
        {
            return actorToNetEnemy.Count;
        }
        
        public void RestartRun()
        {
            // Despawn enemies you've spawned (keep references in a list as you spawn them)
            DespawnAllEnemies();
            
            // Recreate sim + reset counters
            sim = new ServerSim(seed);
            sim.Start();
            elapsed = 0f;
            spawnTimer = 0f;
            netTeamScore.Value = 0;
            netGameOver.Value = false;
            
            clientToActor.Clear();
            // Re-register players: give them actors and reset their views
            foreach (var connIdToNetClient in NetworkManager.ConnectedClients)
            {
                var playerNetObject = connIdToNetClient.Value.PlayerObject;
                var netPlayer = playerNetObject ? playerNetObject.GetComponent<NetPlayer>() : null;
                if (netPlayer != null)
                {
                    RegisterNetPlayerServer(netPlayer); // spawns domain player
                }
            }
        }
        
        private void DespawnAllEnemies()
        {
            var toDespawn = new List<NetEnemy>();
            foreach (var enemy in actorToNetEnemy.Values)
            {
                if (enemy != null && enemy.IsSpawned)
                {
                    toDespawn.Add(enemy);
                }
            }
            
            foreach (var enemy in toDespawn)
            {
                enemy.NetworkObject.Despawn(true);
            }
            actorToNetEnemy.Clear();
        }
        public void Exit()
        {
            RestartRun();
            sim = null;
            NetworkManager.Singleton.Shutdown();
        }
    }
}
