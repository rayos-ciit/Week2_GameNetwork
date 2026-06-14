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

        if (GameManager.Instance != null && GameManager.Instance.matchState.Value != 2)
        {
            isCharging = false;
            currentCharge = 0f;
            if (chargeSlider != null) chargeSlider.value = 0f;
            return;
        }

        if (fastChargeTimer > 0)
        {
            fastChargeTimer -= Time.deltaTime;
            chargeRate = 2f; 
        }
        else chargeRate = 1f;

        if (Input.GetKey(fireKey))
        {
            isCharging = true;
            currentCharge += Time.deltaTime * chargeRate;
            currentCharge = Mathf.Clamp(currentCharge, 0f, maxChargeTime);
        }
        else if (Input.GetKeyUp(fireKey) && isCharging)
        {
            float chargePct = currentCharge / maxChargeTime;
            
            if (chargePct >= 0.2f)
            {
                // CROSSHAIR AIMING LOGIC
                Vector3 aimDirection = bulletSpawnPoint.forward;
                if (Camera.main != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                    {
                        aimDirection = (hit.point - bulletSpawnPoint.position).normalized; // Aim at exactly what you hit
                    }
                    else
                    {
                        aimDirection = (ray.GetPoint(100f) - bulletSpawnPoint.position).normalized; // Aim far off into the sky
                    }
                }

                RequestShootServerRpc(bulletSpawnPoint.position, aimDirection, chargePct, OwnerClientId);
            }
            
            isCharging = false;
            currentCharge = 0f;
        }

        if (chargeSlider != null) chargeSlider.value = currentCharge / maxChargeTime;
    }

    public void ApplyFastCharge(float duration)
    {
        fastChargeTimer = duration;
    }

    [ServerRpc]
    private void RequestShootServerRpc(Vector3 spawnPosition, Vector3 spawnDirection, float chargePct, ulong shooterId)
    {
        GameObject projectileInstance = Instantiate(bulletPrefab, spawnPosition, Quaternion.LookRotation(spawnDirection));
        
        // WE MOVED THIS UP: We MUST spawn the object on the network first...
        NetworkObject networkObject = projectileInstance.GetComponent<NetworkObject>();
        networkObject.Spawn();

        // ...BEFORE we initialize it, so the NetworkVariables are allowed to accept the data!
        NetworkProjectile projScript = projectileInstance.GetComponent<NetworkProjectile>();
        if (projScript != null)
        {
            projScript.Initialize(chargePct, shooterId);
        }
    }
}