using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerCounter : MonoBehaviour
{
    public TextMeshProUGUI counterText;

    void Update()
    {
        if (NetworkManager.Singleton != null)
        {
            // Gets the number of currently connected clients
            int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
            counterText.text = "Players: " + playerCount;
        }
    }
}