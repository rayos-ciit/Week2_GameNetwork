using UnityEngine;
using Unity.Netcode;

public class PowerUp : NetworkBehaviour
{
    public enum PowerUpType { Health, FastCharge }
    public PowerUpType type;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            if (type == PowerUpType.Health)
            {
                NetworkPlayerHealth health = other.GetComponent<NetworkPlayerHealth>();
                if (health != null) health.TakeDamage(-30, 999); // Heal by dealing negative damage
            }
            else if (type == PowerUpType.FastCharge)
            {
                NetworkPlayerShooter shooter = other.GetComponent<NetworkPlayerShooter>();
                if (shooter != null) shooter.ApplyFastCharge(10f); // 10 seconds of fast charging
            }

            NetworkObject.Despawn();
        }
    }
}