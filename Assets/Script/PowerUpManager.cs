using UnityEngine;
using Unity.Netcode;

public class PowerUpManager : NetworkBehaviour
{
    [SerializeField] private GameObject[] powerUpPrefabs;
    [SerializeField] private Vector2 spawnAreaMin; // e.g., -10, -10
    [SerializeField] private Vector2 spawnAreaMax; // e.g., 10, 10
    [SerializeField] private float spawnInterval = 15f;

    private float timer;

    void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnPowerUp();
            timer = 0f;
        }
    }

    private void SpawnPowerUp()
    {
        Vector3 randomPos = new Vector3(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            1f, // Height just above the floor
            Random.Range(spawnAreaMin.y, spawnAreaMax.y)
        );

        int index = Random.Range(0, powerUpPrefabs.Length);
        GameObject powerUp = Instantiate(powerUpPrefabs[index], randomPos, Quaternion.identity);
        powerUp.GetComponent<NetworkObject>().Spawn();
    }
}