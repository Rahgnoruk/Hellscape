using Unity.Netcode;
using UnityEngine;


namespace Hellscape.Net
{
    [RequireComponent(typeof(Hellscape.Gameplay.PlayerInputHandler))]
    public sealed class NetworkPlayerEnable : NetworkBehaviour
    {
        void Start()
        {
            var input = GetComponent<Hellscape.Gameplay.PlayerInputHandler>();
            input.enabled = IsOwner; // only local owner reads input
        }
    }
}