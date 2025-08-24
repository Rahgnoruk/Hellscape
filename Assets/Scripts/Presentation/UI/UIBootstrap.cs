using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
namespace Hellscape.Presentation.UI
{
    public class UIBootstrap : MonoBehaviour
    {
        [Header("UI Documents")]
        [SerializeField] private UIDocument mainMenuDoc;
        [SerializeField] private UIDocument hudDoc;
        [SerializeField] private UIDocument pauseDoc;
        
        [Header("UI Components")]
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private HudUI hudUI;
        [SerializeField] private VignetteFxUI vignetteFxUI;
        [SerializeField] private PauseMenuUI pauseMenuUI;
        
        private void Awake()
        {
            // Make UI root persistent
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            // Initial state: show main menu, hide others
            ShowMainMenu();
            
            // Subscribe to network events
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
        
        private void OnNetworkConnected(ulong clientId)
        {
            // When network connects (either as host or client), show HUD and hide main menu
            ShowGameUI();
        }
        
        private void OnNetworkDisconnected(ulong clientId)
        {
            // When network disconnects, show main menu and hide game UI
            ShowMainMenu();
        }
        
        private void ShowMainMenu()
        {
            if (mainMenuDoc != null) mainMenuDoc.gameObject.SetActive(true);
            if (hudDoc != null) hudDoc.gameObject.SetActive(false);
            if (pauseDoc != null) pauseDoc.gameObject.SetActive(false);
            
            if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(true);
            if (hudUI != null) hudUI.gameObject.SetActive(false);
            if (vignetteFxUI != null) vignetteFxUI.gameObject.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(false);
        }
        
        private void ShowGameUI()
        {
            if (mainMenuDoc != null) mainMenuDoc.gameObject.SetActive(false);
            if (hudDoc != null) hudDoc.gameObject.SetActive(true);
            if (pauseDoc != null) pauseDoc.gameObject.SetActive(false);
            
            if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(false);
            if (hudUI != null) hudUI.gameObject.SetActive(true);
            if (vignetteFxUI != null) vignetteFxUI.gameObject.SetActive(true);
            if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(true);
        }
        
        // Public methods for external access
        public void ShowPauseMenu(bool show)
        {
            if (pauseMenuUI != null)
            {
                pauseMenuUI.Show(show);
            }
        }
        
        public HudUI GetHudUI()
        {
            return hudUI;
        }
        
        public VignetteFxUI GetVignetteFxUI()
        {
            return vignetteFxUI;
        }
        
        public MainMenuUI GetMainMenuUI()
        {
            return mainMenuUI;
        }
    }
}
