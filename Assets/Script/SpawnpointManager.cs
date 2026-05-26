using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

//creates a network aware spawn manager script
public class SpawnpointManager : NetworkBehaviour
{

    //stores which spawn point should be used next
    private static int nextSpawnIndex;



    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("Spawnpoint");
        if (spawnPointObjects.Length == 0)
        {
            Debug.Log("no spawnpoint detected");
            return;
        }

        Transform selectedSpawnPoint = spawnPointObjects[nextSpawnIndex].transform;
        CharacterController characterController = GetComponent<CharacterController>();

        if (characterController != null)
        {

            characterController.enabled = false;

        }
        transform.position = selectedSpawnPoint.position;
        transform.rotation = selectedSpawnPoint.rotation;

        if (characterController != null)
        {

            characterController.enabled = true;

        }

        nextSpawnIndex++;
        if (nextSpawnIndex >= spawnPointObjects.Length) 
        { 
            nextSpawnIndex = 0; 
        }

    }
}
