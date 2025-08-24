using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core.Environments; // RelayServerData

namespace Hellscape.Net
{
    public sealed class RelayBootstrap : MonoBehaviour
    {

        [SerializeField] string environment = "production"; // or "staging", per UGS project
        [SerializeField] int maxConnections = 16; // host + N clients


        string lastJoinCode = string.Empty;
        string joinCodeInput = string.Empty;
        bool servicesReady;

        // Public read-only property for UI access
        public string LastJoinCode => lastJoinCode;


        async void Awake()
        {
            await EnsureServicesAsync();
        }


        async Task EnsureServicesAsync()
        {
            if (servicesReady) return;
            try
            {
                var opts = new InitializationOptions().SetEnvironmentName(environment);
                await UnityServices.InitializeAsync(opts);
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                servicesReady = true;
                Debug.Log($"UGS initialized. PlayerId={AuthenticationService.Instance.PlayerId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }


        public async Task StartHostAsync()
        {
            await EnsureServicesAsync();
            try
            {
                // 1) Create relay allocation (auto region)
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                // 2) Get join code
                lastJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                // 3) Configure transport
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
#if UNITY_WEBGL
                var rsd = allocation.ToRelayServerData("wss"); // WebGL → secure WebSockets
#else
                var rsd = allocation.ToRelayServerData("dtls"); // Native → UDP/DTLS
#endif
#if UNITY_TRANSPORT_2_0_0_OR_NEWER
                transport.SetRelayServerData(rsd);
#else
                transport.SetRelayServerData(rsd);
#endif
                // 4) Start host
                if (!NetworkManager.Singleton.StartHost())
                {
                    Debug.LogError("StartHost failed");
                    lastJoinCode = string.Empty;
                }
                else
                {
                    Debug.Log($"Relay Host started. Join Code: {lastJoinCode}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                lastJoinCode = string.Empty;
            }
        }

        public async Task StartClientAsync(string code)
        {
            await EnsureServicesAsync();
            try
            {
                // 1) Join allocation using code
                JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(code);
                // 2) Configure transport
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
#if UNITY_WEBGL
                var rsd = join.ToRelayServerData("wss");
#else
                var rsd = join.ToRelayServerData("dtls");
#endif
#if UNITY_TRANSPORT_2_0_0_OR_NEWER
                transport.SetRelayServerData(rsd);
#else
                transport.SetRelayServerData(rsd);
#endif
                // 3) Start client
                if (!NetworkManager.Singleton.StartClient())
                {
                    Debug.LogError("StartClient failed");
                }
                else
                {
                    Debug.Log("Client started via Relay");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}