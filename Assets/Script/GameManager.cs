using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Header("UI Elements")]
    public TMP_Text timerText;
    public TMP_Text statusText; 
    public Button restartButton;
    public TMP_Text p1ScoreText;
    public TMP_Text p2ScoreText;

    public NetworkVariable<int> hostScore = new NetworkVariable<int>(0);
    public NetworkVariable<int> clientScore = new NetworkVariable<int>(0);
    public NetworkVariable<float> timeRemaining = new NetworkVariable<float>(90f);
    
    public NetworkVariable<int> matchState = new NetworkVariable<int>(0); 
    public NetworkVariable<float> countdownTimer = new NetworkVariable<float>(3.99f);

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // NEW: Hide the UI completely when the game first boots up
        if (timerText) timerText.gameObject.SetActive(false);
        if (p1ScoreText) p1ScoreText.gameObject.SetActive(false);
        if (p2ScoreText) p2ScoreText.gameObject.SetActive(false);
        if (statusText) statusText.gameObject.SetActive(false);
        if (restartButton) restartButton.gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        // NEW: Once connected (Host or Join), turn the core UI back on!
        if (timerText) timerText.gameObject.SetActive(true);
        if (p1ScoreText) p1ScoreText.gameObject.SetActive(true);
        if (p2ScoreText) p2ScoreText.gameObject.SetActive(true);

        restartButton.gameObject.SetActive(false);
        restartButton.onClick.AddListener(() => { RequestRestartServerRpc(); });
    }

    void Update()
    {
        // Do not update or show anything until the player has actually clicked Host/Client!
        if (!IsSpawned) return;

        UpdateUI();

        if (!IsServer) return;

        if (matchState.Value == 0) // Waiting for players
        {
            if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
            {
                matchState.Value = 1; // Start Countdown
            }
        }
        else if (matchState.Value == 1) // Countdown
        {
            countdownTimer.Value -= Time.deltaTime;
            if (countdownTimer.Value <= 0)
            {
                matchState.Value = 2; // Start Match!
            }
        }
        else if (matchState.Value == 2) // Playing
        {
            timeRemaining.Value -= Time.deltaTime;
            if (timeRemaining.Value <= 0 || hostScore.Value >= 5 || clientScore.Value >= 5)
            {
                EndMatch();
            }
        }
    }

    public void AddScore(ulong killerId)
    {
        if (!IsServer || matchState.Value != 2) return;

        if (killerId == 0) hostScore.Value++;
        else clientScore.Value++;
    }

    private void EndMatch()
    {
        matchState.Value = 3; // Game Over
        timeRemaining.Value = 0f;
    }

    private void UpdateUI()
    {
        int minutes = Mathf.Max(0, Mathf.FloorToInt(timeRemaining.Value / 60));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(timeRemaining.Value % 60));
        timerText.text = $"{minutes:00}:{seconds:00}";

        p1ScoreText.text = $"P1: {hostScore.Value}";
        p2ScoreText.text = $"P2: {clientScore.Value}";

        if (matchState.Value == 0)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Waiting for Player 2...";
        }
        else if (matchState.Value == 1)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = Mathf.FloorToInt(countdownTimer.Value).ToString();
            restartButton.gameObject.SetActive(false); // <-- FIX: Hide button during countdown
        }
        else if (matchState.Value == 2)
        {
            statusText.gameObject.SetActive(false); 
            restartButton.gameObject.SetActive(false); // <-- FIX: Keep it hidden during gameplay
        }
        else if (matchState.Value == 3)
        {
            statusText.gameObject.SetActive(true);
            restartButton.gameObject.SetActive(true); // Shows up only on Game Over

            if (hostScore.Value > clientScore.Value) statusText.text = "Player 1 Wins!";
            else if (clientScore.Value > hostScore.Value) statusText.text = "Player 2 Wins!";
            else statusText.text = "Draw!";
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestRestartServerRpc()
    {
        hostScore.Value = 0;
        clientScore.Value = 0;
        timeRemaining.Value = 90f;
        countdownTimer.Value = 3.99f;
        matchState.Value = 1; // Go straight back to countdown

        NetworkPlayerHealth[] players = FindObjectsOfType<NetworkPlayerHealth>();
        foreach (var p in players) 
        {
            p.ClearSpawnLock();
            p.Respawn();
        }
    }
}