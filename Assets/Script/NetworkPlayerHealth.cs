using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class NetworkPlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    [Header("UI Elements (Local Only)")]
    [SerializeField] private GameObject localUIContainer; 
    [SerializeField] private TMP_Text healthText; 
    [SerializeField] private Slider healthSlider;

    [Header("Floating Damage Settings")]
    [SerializeField] private GameObject damageTextPrefab; 

    //Network-synced health variable
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server 
    );
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth; 
        }

        if (localUIContainer != null)
        {
            localUIContainer.SetActive(IsOwner);
        }
        currentHealth.OnValueChanged += OnHealthChanged; 
        UpdateHealthUI(currentHealth.Value); 
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged; 
    }

    public void OnHealthChanged(int previousValue, int newValue)
    {
        UpdateHealthUI(newValue); 
    }

    private void UpdateHealthUI(int healthValue)
    {
        if(!IsOwner) return;

        if (healthText != null) healthText.text = $"HP: {healthValue}/{maxHealth}";
        if (healthSlider != null) healthSlider.value = (float)healthValue / maxHealth;
    }

    // UPDATE: Now accepts the shooterId to track who gets the kill
    public void TakeDamage(int damage, ulong shooterId)
    {
        if (!IsServer) return; //Only the server should modify health

        currentHealth.Value -= damage; 
        
        // Ensure health doesn't go below 0 or above maxHealth (important for the Health PowerUp!)
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

        // Tell all clients to spawn a floating damage text
        SpawnDamageTextClientRpc(damage, transform.position);

        if (currentHealth.Value <= 0)
        {
            // Player has died!
            
            // Give a point to the killer (If the ID is 999, it means a powerup killed them, which shouldn't happen, but we check just in case!)
            if (GameManager.Instance != null && shooterId != 999)
            {
                GameManager.Instance.AddScore(shooterId);
            }

            Respawn(); 
        }
    }

    [ClientRpc]
    private void SpawnDamageTextClientRpc(int damageAmount, Vector3 spawnPosition)
    {
        if (damageTextPrefab == null) return;

        // Don't spawn text if it was a health powerup (healing is negative damage)
        if (damageAmount < 0) return;

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
        currentHealth.Value = maxHealth; 
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        
        if (spawnPointObjects.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPointObjects.Length);
            Transform selectedSpawnPoint = spawnPointObjects[randomIndex].transform;

            CharacterController characterController = GetComponent<CharacterController>();

            if (characterController != null) characterController.enabled = false; 

            transform.position = selectedSpawnPoint.position; 
            transform.rotation = selectedSpawnPoint.rotation; 

            if (characterController != null) characterController.enabled = true; 
        }
    }
}