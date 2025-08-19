using UnityEngine;


namespace Hellscape.Core {
    public enum GameMode { SinglePlayer /*, HostMP, ClientMP*/ }


    public sealed class GameApp : MonoBehaviour {
        [SerializeField] private GameMode mode = GameMode.SinglePlayer;
        [SerializeField] private int seed = 0;


        private ServerSim server;
        private ITransport transport;


        void Awake() {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;


            if (mode == GameMode.SinglePlayer) {
                transport = new LocalTransport();
                server = new ServerSim(transport, seed == 0 ? System.Environment.TickCount : seed);
                server.Start();
            }
        }


        void Update() {
        // Visual-only updates live here (camera, UI)
        }


        void FixedUpdate() {
            // Drive the sim at fixed rate
            server?.Tick();
        }
    }
}