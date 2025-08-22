using UnityEngine;
using Unity.Netcode;
using Hellscape.App;

public sealed class HudScore : MonoBehaviour
{
    [SerializeField] SimGameServer server; // assign in scene

    void OnGUI()
    {
        if (server == null) return;
        if (!NetworkManager.Singleton) return;
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient) return;

        int score = server.netTeamScore.Value;
        float t = server.netReviveSeconds.Value;
        int dead = server.netDeadAwaiting.Value;

        GUI.Label(new Rect(10, 10, 300, 24), $"Score: {score}");
        if (dead > 0 && t > 0.01f)
            GUI.Label(new Rect(10, 34, 400, 24), $"{dead} awaiting revival — {t:0.0}s");
    }
}
