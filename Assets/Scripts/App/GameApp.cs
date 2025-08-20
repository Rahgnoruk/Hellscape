using UnityEngine;
using Hellscape.Domain;
using Hellscape.Platform;
using Vector2 = Hellscape.Domain.Vector2;
namespace Hellscape.App {
    public enum GameMode { SinglePlayer , HostMP, ClientMP }

    public sealed class GameApp : MonoBehaviour {
        [SerializeField] private GameMode mode = GameMode.SinglePlayer;
        [SerializeField] private int seed = 42;
        
        private ServerSim serverSim;
        private UnityClock clock;
        private InputAdapter inputAdapter;

        void Awake() {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;

            if (mode == GameMode.SinglePlayer) {
                // Initialize domain simulation
                serverSim = new ServerSim(seed);
                serverSim.Start();
                int playerId = serverSim.SpawnPlayerAt(new Vector2(0, 0)); // Spawn player at origin

                // Initialize platform adapters
                clock = new UnityClock();
                
                // Create input adapter
                inputAdapter = gameObject.AddComponent<InputAdapter>();
                inputAdapter.Initialize(serverSim, playerId);
            }
        }

        void FixedUpdate() {
            serverSim?.Tick(clock.FixedDelta);
        }
    }
}