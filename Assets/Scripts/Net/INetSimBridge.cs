using UnityEngine;

namespace Hellscape.Net
{
    // Implemented by the App layer (SimGameServer).
    public interface INetSimBridge
    {
        void RegisterNetPlayerServer(NetPlayer player);
        void SubmitInputFrom(ulong clientId, Vector2 move, Vector2 aim, byte buttons = 0);
    }

    // Static hook where the App layer installs its bridge instance.
    public static class NetSim
    {
        public static INetSimBridge Bridge;
    }
}