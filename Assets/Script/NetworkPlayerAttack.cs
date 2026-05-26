using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerAttack : NetworkBehaviour
{
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private LayerMask playerLayer; //Layer mask to specify what can be attacked
    [SerializeField] private KeyCode playerAttackKey = KeyCode.Space; //Key to perform attack

    // Update is called once per frame
    void Update()
    {
        if(!IsOwner) return; //Only the owning client should check for input
        if (Input.GetKeyDown(playerAttackKey))
        {
            RequestAttackServerRpc(); //Request the server to perform the attack logic
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
                targetHealth.TakeDamage(attackDamage); //Apply damage to the target player
                Debug.Log($"Attacked {hit.name} for {attackDamage} damage");
                break; //Only attack the first valid target hit
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward, attackRange);
    }
}