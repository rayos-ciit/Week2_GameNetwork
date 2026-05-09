using Unity.Netcode;
using UnityEngine;

public class MultiplayerMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuUI; // Drag your UI Panel here in the Inspector

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        HideUI();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        HideUI();
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        HideUI();
    }

    private void HideUI()
    {
        if (menuUI != null)
        {
            menuUI.SetActive(false);
        }
    }
}