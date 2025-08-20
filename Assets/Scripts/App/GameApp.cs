using UnityEngine;


namespace Hellscape.Core {
    public enum GameMode { SinglePlayer /*, HostMP, ClientMP*/ }


    public sealed class GameApp : MonoBehaviour {
        [SerializeField] private GameMode mode = GameMode.SinglePlayer;
        [SerializeField] private int seed = 0;




        void Awake() {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;


            if (mode == GameMode.SinglePlayer) {
            }
        }


        void Update() {
        // Visual-only updates live here (camera, UI)
        }


        void FixedUpdate() {
        }
    }
}