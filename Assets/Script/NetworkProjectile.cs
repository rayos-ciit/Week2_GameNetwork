using UnityEngine;
using Unity.Netcode;

public class NetworkProjectile : NetworkBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float lifeTime;
    private float despawnTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            despawnTime = Time.time + lifeTime;
        }
    }

    void Update()
    {
        if (!IsServer) {return;}

        transform.position += transform.forward * speed * Time.deltaTime;
        if (Time.time > despawnTime)
        {
            NetworkObject.Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!IsServer) {return;}
        if(other.CompareTag("Player"))
        {
            NetworkObject.Despawn();
        }
    }
}