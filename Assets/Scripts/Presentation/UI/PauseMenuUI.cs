using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

namespace Hellscape.Presentation.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuUI : MonoBehaviour
    {
        private UIDocument document;
        private Button btnResume;
        private Button btnExit;
        
        private void Awake()
        {
            document = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            SetupUI();
            Show(false); // Start hidden
        }
        
        private void SetupUI()
        {
            var root = document.rootVisualElement;
            
            btnResume = root.Q<Button>("BtnResume");
            btnExit = root.Q<Button>("BtnExit");
            
            // Wire up button events
            btnResume.clicked += OnResumeClicked;
            btnExit.clicked += OnExitClicked;
        }
        
        public void Show(bool show)
        {
            gameObject.SetActive(show);
            
            if (show)
            {
                // Pause the game
                Time.timeScale = 0f;
            }
            else
            {
                // Resume the game
                Time.timeScale = 1f;
            }
        }
        
        private void OnResumeClicked()
        {
            Show(false);
        }
        
        private void OnExitClicked()
        {
            // Resume time scale before shutting down
            Time.timeScale = 1f;
            
            // Shutdown network if running
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
            
            // Hide pause menu
            Show(false);
            
            // Show main menu and hide HUD (handled by UIBootstrap)
            var mainMenu = FindObjectOfType<MainMenuUI>();
            if (mainMenu != null)
            {
                mainMenu.gameObject.SetActive(true);
            }
            
            var hud = FindObjectOfType<HudUI>();
            if (hud != null)
            {
                hud.gameObject.SetActive(false);
            }
        }
        
        private void OnDestroy()
        {
            // Ensure time scale is restored if destroyed
            Time.timeScale = 1f;
        }
    }
}
