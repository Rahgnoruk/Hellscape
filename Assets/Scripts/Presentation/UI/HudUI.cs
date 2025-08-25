using Hellscape.App;
using Hellscape.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hellscape.Presentation.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HudUI : MonoBehaviour
    {

        [SerializeField] private RelayBootstrap relayBootstrap;
        
        private UIDocument document;
        private Label teamScoreText;
        private Label personalScoreText;
        private Label joinCodeText;
        private Label healthText;
        private VisualElement healthBarFill;
        private Button btnMenu;
        private VisualElement[] inventorySlots;
        
        // Health placeholder (will be replaced with real health system later)
        private int currentHealth = 100;
        private int maxHealth = 100;
        
        private void Awake()
        {
            document = GetComponent<UIDocument>();
        }
        
        private void Start()
        {
            SetupUI();
            UpdateHealthDisplay();
        }
        
        private void Update()
        {
            UpdateScores();
        }
        
        private void SetupUI()
        {
            var root = document.rootVisualElement;
            
            teamScoreText = root.Q<Label>("TeamScoreText");
            personalScoreText = root.Q<Label>("PersonalScoreText");
            joinCodeText = root.Q<Label>("JoinCodeText");
            joinCodeText.text = $"Join Code: {relayBootstrap.LastJoinCode}";
            healthText = root.Q<Label>("HealthText");
            healthBarFill = root.Q<VisualElement>("HealthBarFill");
            btnMenu = root.Q<Button>("BtnMenu");
            
            // Setup inventory slots
            inventorySlots = new VisualElement[4];
            for (int i = 0; i < 4; i++)
            {
                inventorySlots[i] = root.Q<VisualElement>($"InvSlot{i}");
            }
            
            // Wire up menu button
            btnMenu.clicked += OnMenuClicked;
            
            // Initialize inventory slots
            SetSlot(0, "Base", 0, true);
            SetSlot(1, "Empty", 0, false);
            SetSlot(2, "Empty", 0, false);
            SetSlot(3, "Empty", 0, false);
        }
        
        private void UpdateScores()
        {
            if (SimGameServer.Instance != null && SimGameServer.Instance.IsSpawned)
            {
                // Update team score
                int teamScore = SimGameServer.Instance.netTeamScore.Value;
                teamScoreText.text = $"Team: {teamScore:D6}";
                
                // Find personal score for local client
                int personalScore = 0;
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                
                // For now, we'll use a placeholder personal score
                // TODO: Implement proper personal score tracking
                personalScore = teamScore / Mathf.Max(1, NetworkManager.Singleton.ConnectedClients.Count);
                personalScoreText.text = $"You: {personalScore:D4}";
            }
            else
            {
                teamScoreText.text = "Team: 000000";
                personalScoreText.text = "You: 0000";
            }
        }
        
        public void SetLocalHealth(int current, int max)
        {
            currentHealth = Mathf.Clamp(current, 0, max);
            maxHealth = max;
            UpdateHealthDisplay();
        }
        
        private void UpdateHealthDisplay()
        {
            if (healthText != null)
            {
                healthText.text = $"HP {currentHealth}/{maxHealth}";
            }
            
            if (healthBarFill != null)
            {
                float healthPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
                healthBarFill.style.width = Length.Percent(healthPercent * 100f);
                
                // Change color based on health percentage
                if (healthPercent > 0.6f)
                {
                    healthBarFill.style.backgroundColor = new Color(0f, 1f, 0f, 1f); // Green
                }
                else if (healthPercent > 0.3f)
                {
                    healthBarFill.style.backgroundColor = new Color(1f, 1f, 0f, 1f); // Yellow
                }
                else
                {
                    healthBarFill.style.backgroundColor = new Color(1f, 0f, 0f, 1f); // Red
                }
            }
        }
        
        public void SetSlot(int slotIndex, string label, int ammo, bool active)
        {
            if (slotIndex < 0 || slotIndex >= inventorySlots.Length || inventorySlots[slotIndex] == null)
                return;
                
            var slot = inventorySlots[slotIndex];
            
            // Find the label element within the slot
            var labelElement = slot.Q<Label>();
            if (labelElement != null)
            {
                labelElement.text = label;
            }
            
            // Update active state
            if (active)
            {
                slot.RemoveFromClassList("slot");
                slot.AddToClassList("slot--active");
            }
            else
            {
                slot.RemoveFromClassList("slot--active");
                slot.AddToClassList("slot");
            }
        }
        
        private void OnMenuClicked()
        {
            
        }
    }
}
