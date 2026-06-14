using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI Elements")]
    public TMP_Text timerText;
    public TMP_Text matchResultText;
    public Button restartButton;
    public TMP_Text p1ScoreText;
    public TMP_Text p2ScoreText;

    public NetworkVariable<int> hostScore = new NetworkVariable<int>(0);
    public NetworkVariable<int> clientScore = new NetworkVariable<int>(0);
    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(90f); // 1 minute 30 seconds
    public NetworkVariable<bool> isMatchOver = new NetworkVariable<bool>(false);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        restartButton.gameObject.SetActive(false);
        matchResultText.gameObject.SetActive(false);
        
        restartButton.onClick.AddListener(() => {
            RequestRestartServerRpc();
        });
    }

    void Update()
    {
        UpdateUI();

        if (!IsServer || isMatchOver.Value) return;

        timeRemaining.Value -= Time.deltaTime;

        if (timeRemaining.Value <= 0 || hostScore.Value >= 5 || clientScore.Value >= 5)
        {
            EndMatch();
        }
    }

    public void AddScore(ulong killerId)
    {
        if (!IsServer || isMatchOver.Value) return;

        if (killerId == 0) hostScore.Value++;
        else clientScore.Value++;
    }

    private void EndMatch()
    {
        isMatchOver.Value = true;
        timeRemaining.Value = 0f;
    }

    private void UpdateUI()
    {
        int minutes = Mathf.FloorToInt(timeRemaining.Value / 60);
        int seconds = Mathf.FloorToInt(timeRemaining.Value % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";

        p1ScoreText.text = $"P1: {hostScore.Value}";
        p2ScoreText.text = $"P2: {clientScore.Value}";

        if (isMatchOver.Value)
        {
            matchResultText.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(true);

            if (hostScore.Value > clientScore.Value) matchResultText.text = "Player 1 Wins!";
            else if (clientScore.Value > hostScore.Value) matchResultText.text = "Player 2 Wins!";
            else matchResultText.text = "Draw!";
        }
        else
        {
            matchResultText.gameObject.SetActive(false);
            restartButton.gameObject.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRestartServerRpc()
    {
        hostScore.Value = 0;
        clientScore.Value = 0;
        timeRemaining.Value = 90f;
        isMatchOver.Value = false;

        // Tell all clients to respawn via ClientRpc or directly reset positions
        NetworkPlayerHealth[] players = FindObjectsOfType<NetworkPlayerHealth>();
        foreach (var p in players) p.Respawn();
    }
}