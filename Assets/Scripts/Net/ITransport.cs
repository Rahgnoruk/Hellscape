using UnityEngine;
namespace Hellscape.Core {
    public interface ITransport
    {
        void SendToServer(byte[] msg);
        void SendToClient(byte[] msg);
        System.Action<byte[]> OnServerMsg { get; set; }
        System.Action<byte[]> OnClientMsg { get; set; }
    }
}