using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class NetworkPlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    [Header("UI Elements (Local Only)")]
    [SerializeField] private GameObject localUIContainer; // Container for local player's UI elements
    [SerializeField] private TMP_Text healthText; // Text element to display health
    [SerializeField] private Slider healthSlider;

    [Header("Floating Damage Settings")]
    [SerializeField] private GameObject damageTextPrefab; // Prefab for floating damage text

    //Network-synced health variable
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone, //The host/Client/Server can read this variable
        NetworkVariableWritePermission.Server //Only the server can change this value
    );
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth; //Initialize health on the server
        }

        if (localUIContainer != null)
        {
            localUIContainer.SetActive(IsOwner);
        }
        currentHealth.OnValueChanged += OnHealthChanged; //Subscribe to health change events
        UpdateHealthUI(currentHealth.Value); //Initial UI update
    }

    public override void OnNetworkDespawn()
    {
        //Optional: Handle any cleanup when the object is despawned
        currentHealth.OnValueChanged -= OnHealthChanged; //Unsubscribe from health change events
    }

    public void OnHealthChanged(int previousValue, int newValue)
    {
        //This method will be called on all clients when the health changes
        Debug.Log($"Health changed from {previousValue} to {newValue}");
        UpdateHealthUI(newValue); //Update the local player's UI

        if (newValue <= 0)
        {
            //Handle player death (e.g., respawn, disable controls, etc.)
            Debug.Log("Player has died!");
        }
    }

    private void UpdateHealthUI(int healthValue)
    {
        if(!IsOwner) return;

        if (healthText != null) healthText.text = $"HP: {healthValue}/{maxHealth}";
        if (healthSlider != null) healthSlider.value = (float)healthValue / maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            
            if (!IsServer) return; //Only the server should modify health

            currentHealth.Value -= damage; //Reduce health by damage amount
            currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

            // Tell all clients to spawn a floating damage text at this position
            SpawnDamageTextClientRpc(damage, transform.position);

            if (currentHealth.Value <= 0)
            {
                currentHealth.Value = 0; //Ensure health doesn't go below zero
                //Handle player death (e.g., respawn, disable controls, etc.)
                Debug.Log("Player has died!");
                Respawn(); //Respawn the player after death
            }
        }
    }

    [ClientRpc]
    private void SpawnDamageTextClientRpc(int damageAmount, Vector3 spawnPosition)
    {
        if (damageTextPrefab == null) return;

        // Spawn text slightly above the player's pivot point
        Vector3 offsetPosition = spawnPosition + Vector3.up * 2f;
        GameObject textInstance = Instantiate(damageTextPrefab, offsetPosition, Quaternion.identity);

        FloatingText damageScript = textInstance.GetComponent<FloatingText>();
        if (damageScript != null)
        {
            damageScript.Initialize(damageAmount);
        }
    }

    public void Respawn()
    {
        currentHealth.Value = maxHealth; //Reset health to max
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        int randomIndex = Random.Range(0, spawnPointObjects.Length);
        Transform selectedSpawnPoint = spawnPointObjects[randomIndex].transform;

        CharacterController characterController = GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false; //Disable character controller before moving
        }

        transform.position = selectedSpawnPoint.position; //Move player to spawn point
        transform.rotation = selectedSpawnPoint.rotation; //Reset player rotation to spawn point

        if (characterController != null)
        {
            characterController.enabled = true; //Re-enable character controller after moving
        }
    }
}