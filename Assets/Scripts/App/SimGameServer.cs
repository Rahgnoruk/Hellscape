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

        private ServerSim sim;
        private readonly Dictionary<ulong, int> clientToActor = new();     // NGO client → Domain actor
        private readonly Dictionary<int, NetPlayer> actorToNetPlayer = new(); // Domain actor → Net view

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
            if (NetSim.Bridge != null) return;
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
        }
        void OnDestroy()
        {
            if (NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            // Uninstall bridge if we’re the one who set it
            if (NetSim.Bridge == this) NetSim.Bridge = null;

            if (Instance == this) Instance = null;
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
        public void RegisterNetPlayerServer(NetPlayer p)
        {
            // Called from NetPlayer.OnNetworkSpawn on the SERVER side
            var cid = p.OwnerClientId;
            if (!clientToActor.TryGetValue(cid, out var actorId))
            {
                // Choose a simple spawn near outskirts
                var spawn = new DVec2(-10, 6);

                actorId = sim.SpawnPlayerAt(spawn);
                clientToActor[cid] = actorId;
            }
            actorToNetPlayer[actorId] = p;
        }
        void OnClientDisconnected(ulong clientId)
        {
            if (clientToActor.TryGetValue(clientId, out var actorId))
            {
                clientToActor.Remove(clientId);
                actorToNetPlayer.Remove(actorId);
                sim.RemoveActor(actorId);
            }
        }

        public void SubmitInputFrom(ulong clientId, Vector2 move, byte buttons = 0)
        {
            if (!IsServer) return;
            if (!clientToActor.TryGetValue(clientId, out var actorId)) return;

            // We’re not using per-actor aim yet; just pass zeros for now.
            var cmd = new InputCommand(
              tick: 0, // not used by sim right now; sim consumes "latest"
              moveX: move.x, moveY: move.y,
              aimX: 0f, aimY: 0f,
              buttons: buttons
            );
            sim.ApplyForActor(actorId ,cmd);
        }

        void FixedUpdate()
        {
            if (!IsServer || sim == null) return;

            // Tick authoritative sim
            sim.Tick(Time.fixedDeltaTime);

            // Push sim positions into replicated NetVariables
            foreach (var kvp in actorToNetPlayer)
            {
                var actorId = kvp.Key;
                var view = kvp.Value;
                if (view == null || !view.IsSpawned) continue;

                if (sim.TryGetActorState(actorId, out var st))
                {
                    view.netPos.Value = new Vector2(st.positionX, st.positionY);
                }
            }
        }
    }
}
