using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ButtonTest : UdonSharpBehaviour {
    public GameObject objectToSpawn;  // The prefab to spawn (with VRC_Pickup and VRC_ObjectSync attached)

    public override void Interact() {
        VRCPlayerApi interactingPlayer = Networking.LocalPlayer;

        if (objectToSpawn == null) {
            Debug.LogError("Button: Object to spawn is not set");
            return;
        } else if (interactingPlayer == null) {
            Debug.LogError("Button: Interacting player is not set");
            return;
        }
        // Spawn the object
        GameObject spawnedObject = Object.Instantiate(objectToSpawn);
        spawnedObject.SetActive(true);
        Shuriken shurikenComponent = spawnedObject.GetComponent<Shuriken>();
        shurikenComponent.SetOwner(interactingPlayer);
        shurikenComponent.ReturnToOwner();
    }
}
