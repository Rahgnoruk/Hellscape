using Hellscape.App;
using Hellscape.Net;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class DefeatScreenUI : MonoBehaviour
{
    [SerializeField] private RelayBootstrap relayBootstrap;

    private UIDocument document;
    private Label teamScoreText;
    private Label personalScoreText;
    private Button restartButton;
    private Button exitButton;
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

        restartButton = root.Q<Button>("RestartButton");
        exitButton = root.Q<Button>("ExitButton");
        teamScoreText = root.Q<Label>("TeamScoreText");
        personalScoreText = root.Q<Label>("PersonalScoreText");

        // Wire up button events
        restartButton.clicked += OnRestartClicked;
        exitButton.clicked += OnExitClicked;

        int teamScore = SimGameServer.Instance.netTeamScore.Value;
        teamScoreText.text = $"Team: {teamScore:D6}";
    }
    private void OnRestartClicked()
    {
        SimGameServer.Instance.RestartRun();
    }
    private void OnExitClicked()
    {
        SimGameServer.Instance.Exit();
    }
}
