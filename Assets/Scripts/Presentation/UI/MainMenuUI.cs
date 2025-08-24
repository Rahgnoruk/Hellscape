using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using Hellscape.Net;

namespace Hellscape.Presentation.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private RelayBootstrap relayBootstrap;
        
        private UIDocument document;
        private Button btnHost;
        private TextField joinCodeField;
        private Button btnJoin;
        private Label statusText;
        
        private void Awake()
        {
            document = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            SetupUI();
            UpdateStatus("Ready to connect");
        }
        
        private void SetupUI()
        {
            var root = document.rootVisualElement;
            
            btnHost = root.Q<Button>("BtnHost");
            joinCodeField = root.Q<TextField>("JoinCode");
            btnJoin = root.Q<Button>("BtnJoin");
            statusText = root.Q<Label>("StatusText");
            
            // Wire up button events
            btnHost.clicked += OnHostClicked;
            btnJoin.clicked += OnJoinClicked;
            
            // Auto-uppercase join code input
            joinCodeField.RegisterValueChangedCallback(evt =>
            {
                joinCodeField.value = evt.newValue.ToUpperInvariant();
            });
            
            // Listen for network state changes
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnNetworkConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnNetworkDisconnected;
            }
        }
        
        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnNetworkConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnNetworkDisconnected;
            }
        }
        
        private async void OnHostClicked()
        {
            if (relayBootstrap == null)
            {
                UpdateStatus("Error: RelayBootstrap not assigned");
                return;
            }
            
            UpdateStatus("Starting host...");
            btnHost.SetEnabled(false);
            btnJoin.SetEnabled(false);
            
            try
            {
                await relayBootstrap.StartHostAsync();
                UpdateStatus($"Host started! Join code: {relayBootstrap.LastJoinCode}");
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"Host failed: {ex.Message}");
                btnHost.SetEnabled(true);
                btnJoin.SetEnabled(true);
            }
        }
        
        private async void OnJoinClicked()
        {
            if (relayBootstrap == null)
            {
                UpdateStatus("Error: RelayBootstrap not assigned");
                return;
            }
            
            string joinCode = joinCodeField.value?.Trim();
            if (string.IsNullOrEmpty(joinCode))
            {
                UpdateStatus("Please enter a join code");
                return;
            }
            
            UpdateStatus("Joining...");
            btnHost.SetEnabled(false);
            btnJoin.SetEnabled(false);
            
            try
            {
                await relayBootstrap.StartClientAsync(joinCode);
                UpdateStatus("Joining game...");
            }
            catch (System.Exception ex)
            {
                UpdateStatus($"Join failed: {ex.Message}");
                btnHost.SetEnabled(true);
                btnJoin.SetEnabled(true);
            }
        }
        private void OnNetworkConnected(ulong clientId)
        {
            // Hide main menu when connected (handled by UIBootstrap)
            gameObject.SetActive(false);
        }
        
        private void OnNetworkDisconnected(ulong clientId)
        {
            // Show main menu when disconnected (handled by UIBootstrap)
            gameObject.SetActive(true);
            UpdateStatus("Disconnected from game");
            btnHost.SetEnabled(true);
            btnJoin.SetEnabled(true);
        }
        
        public void ShowError(string error)
        {
            UpdateStatus($"Error: {error}");
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
