using Hellscape.App;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hellscape.Presentation.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private UIBootstrap uiBootstrap;

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
        
        private void OnResumeClicked()
        {
            uiBootstrap.ShowGameUI();
        }
        
        private void OnExitClicked()
        {
            SimGameServer.Instance.Exit();
        }
    }
}
