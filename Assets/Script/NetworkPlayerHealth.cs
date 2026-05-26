using UnityEngine;
using Unity.Netcode;
public class NetworkPlayerHealth : NetworkBehaviour
{
    [ClientRpc]
    private void ShowDamageClientRpc(int amount)
    {
        if (damageTextPrefab != null)
        {
            GameObject popup = Instantiate(damageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
            popup.GetComponent<TMPro.TMP_Text>().text = amount.ToString();
        }
    }
    
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private int maxHealth = 100;
    //Network-synced health variable
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,//The host/Client/Server can read this variable
        NetworkVariableWritePermission.Server//The server can only change this value
        );
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            CurrentHealth.Value = maxHealth;
        }
        CurrentHealth.OnValueChanged += OnHealthChanged;
    }
    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        Debug.Log($"{gameObject.name} health Change: {previousValue} -> {newValue}");
    }

    public void TakeDamage(int damageAmount)
    {
        if (!IsServer) {return;}    
        CurrentHealth.Value -= damageAmount;
        ShowDamageClientRpc(damageAmount);
        CurrentHealth.Value = Mathf.Clamp(CurrentHealth.Value,0,maxHealth);
        if (CurrentHealth.Value <= 0)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        CurrentHealth.Value = maxHealth;
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("Spawnpoint");
        int randomIndex = Random.Range(0, spawnPointObjects.Length);
        Transform selectedSPawn = spawnPointObjects[randomIndex].transform;

        CharacterController characterController = GetComponent<CharacterController>();

        if (characterController != null)
        {

            characterController.enabled = false;

        }

        transform.position = selectedSPawn.position;
        transform.rotation = selectedSPawn.rotation;

        if (characterController != null)
        {

            characterController.enabled = true;

        }
    }
}
