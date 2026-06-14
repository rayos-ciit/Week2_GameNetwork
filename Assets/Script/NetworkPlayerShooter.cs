using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;

public class NetworkPlayerShooter : NetworkBehaviour
{
    [Header("Shooting & Charge Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private KeyCode fireKey = KeyCode.Mouse0;
    
    public float maxChargeTime = 1.5f;
    public float currentCharge = 0f;
    [HideInInspector] public bool isCharging = false;
    
    [Header("UI")]
    [SerializeField] private Slider chargeSlider; // Assign a UI Slider in the prefab

    private float fastChargeTimer = 0f;
    private float chargeRate = 1f;

    void Update()
    {
        if (!IsOwner) return;

        // Fast Charge Powerup Logic
        if (fastChargeTimer > 0)
        {
            fastChargeTimer -= Time.deltaTime;
            chargeRate = 2f; // Charges twice as fast
        }
        else
        {
            chargeRate = 1f;
        }

        if (Input.GetKey(fireKey))
        {
            isCharging = true;
            currentCharge += Time.deltaTime * chargeRate;
            currentCharge = Mathf.Clamp(currentCharge, 0f, maxChargeTime);
        }
        else if (Input.GetKeyUp(fireKey) && isCharging)
        {
            float chargePercentage = currentCharge / maxChargeTime;
            
            // Only shoot if they charged at least a little bit (e.g., 20%)
            if (chargePercentage >= 0.2f)
            {
                RequestShootServerRpc(bulletSpawnPoint.position, bulletSpawnPoint.forward, chargePercentage, OwnerClientId);
            }
            
            isCharging = false;
            currentCharge = 0f;
        }

        if (chargeSlider != null)
        {
            chargeSlider.value = currentCharge / maxChargeTime;
        }
    }

    public void ApplyFastCharge(float duration)
    {
        fastChargeTimer = duration;
    }

    [ServerRpc]
    private void RequestShootServerRpc(Vector3 spawnPosition, Vector3 spawnDirection, float chargePct, ulong shooterId)
    {
        GameObject projectileInstance = Instantiate(bulletPrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
        
        NetworkProjectile projScript = projectileInstance.GetComponent<NetworkProjectile>();
        if (projScript != null)
        {
            projScript.Initialize(chargePct, shooterId);
        }

        NetworkObject networkObject = projectileInstance.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }
}