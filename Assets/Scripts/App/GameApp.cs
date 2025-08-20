using UnityEngine;
using Hellscape.Domain;
using Hellscape.Platform;
using Hellscape.Net;

namespace Hellscape.App {
    public enum GameMode { SinglePlayer /*, HostMP, ClientMP*/ }

    public sealed class GameApp : MonoBehaviour {
        [SerializeField] private GameMode mode = GameMode.SinglePlayer;
        [SerializeField] private int seed = 42;
        
        private ServerSim serverSim;
        private UnityClock clock;
        private LocalTransport transport;
        private InputAdapter inputAdapter;

        void Awake() {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;

            if (mode == GameMode.SinglePlayer) {
                // Initialize domain simulation
                serverSim = new ServerSim(seed);
                serverSim.Start();
                
                // Initialize platform adapters
                clock = new UnityClock();
                transport = new LocalTransport();
                
                // Create input adapter
                inputAdapter = gameObject.AddComponent<InputAdapter>();
                inputAdapter.Initialize(transport, serverSim);
            }
        }

        void Update() {
            // Visual-only updates live here (camera, UI)
        }

        void FixedUpdate() {
            if (serverSim != null) {
                serverSim.Tick(clock.FixedDelta);
            }
        }
    }
}