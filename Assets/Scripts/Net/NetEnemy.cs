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
        public readonly NetworkVariable<int> actorId =  
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
