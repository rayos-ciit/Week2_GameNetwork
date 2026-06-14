using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerAttack : NetworkBehaviour
{
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask playerLayer; 
    
    // Changed to Mouse1 (Right Click) so it doesn't conflict with your Spacebar Jump!
    [SerializeField] private KeyCode playerAttackKey = KeyCode.Mouse1; 

    void Update()
    {
        if(!IsOwner) return; 
        if (Input.GetKeyDown(playerAttackKey))
        {
            RequestAttackServerRpc(); 
        }
    }

    [ServerRpc]
    private void RequestAttackServerRpc()
    {
        Vector3 attackCenter = transform.position + transform.forward;
        Collider[] hits = Physics.OverlapSphere(attackCenter, attackRange, playerLayer);
        foreach (Collider hit in hits)
        {
            if(hit.gameObject == gameObject) continue; //Skip self
            NetworkPlayerHealth targetHealth = hit.GetComponent<NetworkPlayerHealth>();
            if(targetHealth != null)
            {
                // UPDATE: Added OwnerClientId so you get the point if this melee attack kills them!
                targetHealth.TakeDamage(attackDamage, OwnerClientId); 
                Debug.Log($"Attacked {hit.name} for {attackDamage} damage");
                break; 
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
    }
}