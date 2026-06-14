using UnityEngine;
using Unity.Netcode;

public class NetworkProjectile : NetworkBehaviour
{
    [SerializeField] float baseSpeed = 10f;
    [SerializeField] float lifeTime = 3f;
    [SerializeField] int maxDamage = 50;
    
    private float despawnTime;
    private float currentSpeed;
    private int actualDamage;
    private ulong ownerId;

    public void Initialize(float chargePct, ulong shooterId)
    {
        // Scale speed and damage based on how long the bow was drawn
        currentSpeed = baseSpeed + (baseSpeed * chargePct);
        actualDamage = Mathf.RoundToInt(maxDamage * chargePct);
        ownerId = shooterId;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) despawnTime = Time.time + lifeTime;
    }

    void Update()
    {
        if (!IsServer) return;

        transform.position += transform.forward * currentSpeed * Time.deltaTime;
        if (Time.time > despawnTime) NetworkObject.Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!IsServer) return;

        if(other.CompareTag("Player"))
        {
            NetworkPlayerHealth health = other.GetComponent<NetworkPlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(actualDamage, ownerId);
            }
            NetworkObject.Despawn();
        }
    }
}