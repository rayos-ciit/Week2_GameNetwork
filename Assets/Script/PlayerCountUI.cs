using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerCountUI : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI playerCountText;
    
    // Automatically syncs the integer to all clients
    private NetworkVariable<int> playersOnline = new NetworkVariable<int>(0);

    public override void OnNetworkSpawn()
    {
        playersOnline.OnValueChanged += UpdateUI;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += ClientDisconnected;
            
            // Set initial value for Host
            playersOnline.Value = NetworkManager.Singleton.ConnectedClientsIds.Count;
        }

        // Force UI to update immediately on spawn
        UpdateUI(0, playersOnline.Value);
    }

    public override void OnNetworkDespawn()
    {
        playersOnline.OnValueChanged -= UpdateUI;

        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= ClientDisconnected;
            }
        }
    }

    private void ClientConnected(ulong clientId) { playersOnline.Value++; }
    private void ClientDisconnected(ulong clientId) { playersOnline.Value--; }

    private void UpdateUI(int previous, int current)
    {
        if (playerCountText != null)
        {
            playerCountText.text = "Players Online: " + current;
        }
    }
}