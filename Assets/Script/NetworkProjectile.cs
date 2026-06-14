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

    // NEW: Syncs to all clients if this specific arrow was maxed out
    public NetworkVariable<bool> isMaxCharge = new NetworkVariable<bool>(false);

    public void Initialize(float chargePct, ulong shooterId)
    {
        currentSpeed = baseSpeed + (baseSpeed * chargePct);
        actualDamage = Mathf.RoundToInt(maxDamage * chargePct);
        ownerId = shooterId;

        // If charged to 95% or higher, flag it!
        if (IsServer && chargePct >= 0.95f)
        {
            isMaxCharge.Value = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) despawnTime = Time.time + lifeTime;

        // Listen for changes, and immediately check on spawn
        isMaxCharge.OnValueChanged += OnChargeStateChanged;
        if (isMaxCharge.Value) ApplyMaxChargeVisual();
    }

    public override void OnNetworkDespawn()
    {
        isMaxCharge.OnValueChanged -= OnChargeStateChanged;
    }

    private void OnChargeStateChanged(bool previous, bool current)
    {
        if (current) ApplyMaxChargeVisual();
    }

    private void ApplyMaxChargeVisual()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            r.material.color = Color.red; // Changes the arrow to Red! (You can change this to Color.yellow, etc.)
        }
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
            if (health != null) health.TakeDamage(actualDamage, ownerId);
            NetworkObject.Despawn();
        }
    }
}