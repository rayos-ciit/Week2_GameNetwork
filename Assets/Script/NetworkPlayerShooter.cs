using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerShooter : NetworkBehaviour
{
    [SerializeField] private GameObject bulletPrefab; //bullet prefab to instantiate when shooting
    [SerializeField] private Transform bulletSpawnPoint; //point from which the bullet will be spawned
    [SerializeField] private float fireCooldown = 0.5f; //time in seconds between shots
    [SerializeField] KeyCode fireKey = KeyCode.Mouse0;//key to press for shooting
    private float lastFireTime;

    void Update()
    {

        if (!IsOwner) { return; } //only allow the local player to shoot

        if (Input.GetKeyDown(fireKey) && Time.time >= lastFireTime)
        {

            lastFireTime = Time.time + fireCooldown;

            RequestShootServerRpc(bulletSpawnPoint.position, bulletSpawnPoint.forward);
        }
    }

    [ServerRpc]
    private void RequestShootServerRpc(Vector3 spawnPosition, Vector3 spawnDirection)
    {
        GameObject projectileInstance = Instantiate(
            bulletPrefab,
            spawnPosition,
            Quaternion.LookRotation(spawnDirection)); //we can change the direction so its flexible 

        //Tells the unity netcode to show this object to all players . Calling network object 
        NetworkObject networkObject = projectileInstance.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }
}