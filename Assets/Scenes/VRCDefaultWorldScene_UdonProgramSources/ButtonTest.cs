
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ButtonTest : UdonSharpBehaviour
{
    public GameObject objectToSpawn;  // The prefab to spawn (with VRC_Pickup and VRC_ObjectSync attached)
    public Vector3 handOffset = new Vector3(0.1f, 0.1f, 0.1f); // Adjust to spawn near the player's hand

    public override void Interact()
    {
        VRCPlayerApi interactingPlayer = Networking.LocalPlayer;

        // Ensure the object to spawn is set
        if (objectToSpawn != null && interactingPlayer != null)
        {
            // Spawn the object
            GameObject spawnedObject = VRCInstantiate(objectToSpawn);
            
            // Spawn the object at an offset relative to the player's hand (you can adjust this)
            Vector3 spawnPosition = interactingPlayer.GetPosition() + interactingPlayer.GetRotation() * handOffset;
            spawnedObject.transform.position = spawnPosition;
            spawnedObject.transform.rotation = interactingPlayer.GetRotation();
        }
        else
        {
            Debug.LogError("Object to spawn or interacting player is not set.");
        }
    }
}