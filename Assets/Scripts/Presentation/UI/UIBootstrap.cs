using UnityEngine;
using Unity.Netcode;
using UnityEngine.UIElements;
using Hellscape.App;
namespace Hellscape.Presentation.UI
{
    public class UIBootstrap : MonoBehaviour
    {
        [Header("GameServer")]
        [SerializeField] private SimGameServer gameServer;
        [Header("UI Documents")]
        [SerializeField] private UIDocument mainMenuDoc;
        [SerializeField] private UIDocument hudDoc;
        [SerializeField] private UIDocument pauseDoc;
        [SerializeField] private UIDocument defeatScreenDoc;
        
        [Header("UI Components")]
        [SerializeField] private MainMenuUI mainMenuUI;
        [SerializeField] private HudUI hudUI;
        [SerializeField] private VignetteFxUI vignetteFxUI;
        [SerializeField] private PauseMenuUI pauseMenuUI;
        [SerializeField] private DefeatScreenUI defeatScreenUI;

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
                SimGameServer.Instance.netGameOver.OnValueChanged += OnNetGameOverChanged;
                NetworkManager.Singleton.OnClientConnectedCallback += OnNetworkConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnNetworkDisconnected;
            }
        }
        private void OnDestroy()
        {
            SimGameServer.Instance.netGameOver.OnValueChanged -= OnNetGameOverChanged;
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
            if (defeatScreenDoc != null) defeatScreenDoc.gameObject.SetActive(false);

            if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(true);
            if (hudUI != null) hudUI.gameObject.SetActive(false);
            if (vignetteFxUI != null) vignetteFxUI.gameObject.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(false);
            if (defeatScreenUI != null) defeatScreenUI.gameObject.SetActive(false);
        }
        
        public void ShowGameUI()
        {
            if (hudDoc != null) hudDoc.gameObject.SetActive(true);
            if (mainMenuDoc != null) mainMenuDoc.gameObject.SetActive(false);
            if (pauseDoc != null) pauseDoc.gameObject.SetActive(false);
            if (defeatScreenDoc != null) defeatScreenDoc.gameObject.SetActive(false);

            if (hudUI != null) hudUI.gameObject.SetActive(true);
            if (vignetteFxUI != null) vignetteFxUI.gameObject.SetActive(true);
            if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(false);
            if (defeatScreenUI != null) defeatScreenUI.gameObject.SetActive(false);
        }
        private void OnNetGameOverChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                ShowDefeatScreen();
            }
            else
            {
                ShowGameUI();
            }
        }
        private void ShowDefeatScreen()
        {
            if (defeatScreenDoc != null) defeatScreenDoc.gameObject.SetActive(true);
            if (mainMenuDoc != null) mainMenuDoc.gameObject.SetActive(false);
            if (hudDoc != null) hudDoc.gameObject.SetActive(false);
            if (pauseDoc != null) pauseDoc.gameObject.SetActive(false);
            
            if (defeatScreenUI != null) defeatScreenUI.gameObject.SetActive(true);
            if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(false);
            if (hudUI != null) hudUI.gameObject.SetActive(false);
            if (vignetteFxUI != null) vignetteFxUI.gameObject.SetActive(false);
            if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(false);
        }

        // Public methods for external access
        public void ShowPauseMenu()
        {
            if (pauseDoc != null) pauseDoc.gameObject.SetActive(true);
            if (defeatScreenDoc != null) defeatScreenDoc.gameObject.SetActive(false);
            if (mainMenuDoc != null) mainMenuDoc.gameObject.SetActive(false);
            if (hudDoc != null) hudDoc.gameObject.SetActive(false);

            if (pauseMenuUI != null) pauseMenuUI.gameObject.SetActive(true);
            if (defeatScreenUI != null) defeatScreenUI.gameObject.SetActive(false);
            if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(false);
            if (hudUI != null) hudUI.gameObject.SetActive(false);
            if (vignetteFxUI != null) vignetteFxUI.gameObject.SetActive(false);
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
