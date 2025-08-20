using UnityEngine;
namespace Hellscape.Core {
    public class LocalTransport : ITransport
    {
        public System.Action<byte[]> OnServerMsg { get; set; }
        public System.Action<byte[]> OnClientMsg { get; set; }


        public void SendToServer(byte[] msg) => OnServerMsg?.Invoke(msg);
        public void SendToClient(byte[] msg) => OnClientMsg?.Invoke(msg);
    }
}