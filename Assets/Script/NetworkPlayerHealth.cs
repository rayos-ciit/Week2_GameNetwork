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
    [SerializeField] private GameObject damageTextPrefab; 
    [SerializeField] private AudioClip hitSoundEffect;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> isInvulnerable = new NetworkVariable<bool>(false);
    
    private float invulnerabilityTimer = 0f;
    private const float INVULNERABILITY_DURATION = 2f;

    private Vector3 lockedSpawnPos;
    private Quaternion lockedSpawnRot;
    private bool isSpawnLocked = false;

    private Renderer playerRenderer;
    private Color originalColor;

    public override void OnNetworkSpawn()
    {
        if (localUIContainer != null) localUIContainer.SetActive(IsOwner);
        
        playerRenderer = GetComponentInChildren<Renderer>();
        if (playerRenderer != null) originalColor = playerRenderer.material.color;

        currentHealth.OnValueChanged += OnHealthChanged; 
        isInvulnerable.OnValueChanged += OnInvulnerabilityChanged;

        UpdateHealthUI(currentHealth.Value); 

        if (IsServer) 
        {
            currentHealth.Value = maxHealth; 
            Respawn(); 
        }
    }

    public override void OnNetworkDespawn()
    {
        currentHealth.OnValueChanged -= OnHealthChanged; 
        isInvulnerable.OnValueChanged -= OnInvulnerabilityChanged;
    }

    void Update()
    {
        if (IsServer && invulnerabilityTimer > 0)
        {
            invulnerabilityTimer -= Time.deltaTime;
            if (invulnerabilityTimer <= 0) isInvulnerable.Value = false; 
        }
    }

    private void OnInvulnerabilityChanged(bool previousValue, bool newValue)
    {
        if (playerRenderer != null) playerRenderer.material.color = newValue ? Color.white : originalColor;
    }

    public void OnHealthChanged(int previousValue, int newValue) { UpdateHealthUI(newValue); }

    private void UpdateHealthUI(int healthValue)
    {
        if(!IsOwner) return;
        if (healthText != null) healthText.text = $"HP: {healthValue}/{maxHealth}";
        if (healthSlider != null) healthSlider.value = (float)healthValue / maxHealth;
    }

    public void TakeDamage(int damage, ulong shooterId)
    {
        if (!IsServer) return;
        if (isInvulnerable.Value && damage > 0) return; 

        currentHealth.Value -= damage; 
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);

        SpawnDamageTextClientRpc(damage, transform.position);

        if (currentHealth.Value <= 0)
        {
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
        if (damageTextPrefab == null || damageAmount < 0) return;
        Vector3 offsetPosition = spawnPosition + Vector3.up * 2f;
        GameObject textInstance = Instantiate(damageTextPrefab, offsetPosition, Quaternion.identity);
        FloatingText damageScript = textInstance.GetComponent<FloatingText>();
        if (damageScript != null) damageScript.Initialize(damageAmount);

        if (hitSoundEffect != null) AudioSource.PlayClipAtPoint(hitSoundEffect, spawnPosition);
    }

    public void ClearSpawnLock()
    {
        isSpawnLocked = false;
    }

    public void Respawn()
    {
        if (!IsServer) return; 

        currentHealth.Value = maxHealth; 
        invulnerabilityTimer = INVULNERABILITY_DURATION; 
        isInvulnerable.Value = true;

        if (!isSpawnLocked)
        {
            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawnPointObjects.Length > 0)
            {
                int randomIndex = Random.Range(0, spawnPointObjects.Length);
                lockedSpawnPos = spawnPointObjects[randomIndex].transform.position;
                lockedSpawnRot = spawnPointObjects[randomIndex].transform.rotation;
                isSpawnLocked = true; 
            }
            else
            {
                lockedSpawnPos = transform.position;
                lockedSpawnRot = transform.rotation;
                isSpawnLocked = true;
            }
        }

        // 1. Teleport the Server's authoritative body FIRST so it knows where you went!
        ExecuteTeleport(lockedSpawnPos, lockedSpawnRot);

        // 2. Fire the flare telling all clients to do the exact same thing instantly
        TeleportClientRpc(lockedSpawnPos, lockedSpawnRot);
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 targetPos, Quaternion targetRot)
    {
        // Prevent the Host from doing this twice, since they already moved via the Server code above!
        if (IsServer) return; 
        
        ExecuteTeleport(targetPos, targetRot);
    }

    private void ExecuteTeleport(Vector3 targetPos, Quaternion targetRot)
    {
        lockedSpawnPos = targetPos;
        lockedSpawnRot = targetRot;
        isSpawnLocked = true;

        CharacterController characterController = GetComponent<CharacterController>();
        if (characterController != null) characterController.enabled = false; 

        transform.position = targetPos; 
        transform.rotation = targetRot; 

        if (characterController != null) characterController.enabled = true; 
    }
}