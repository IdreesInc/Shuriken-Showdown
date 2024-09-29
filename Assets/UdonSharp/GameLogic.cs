
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameLogic : UdonSharpBehaviour {

    public GameObject playerCollider;
    public GameObject shuriken;

    void Start() {
        Debug.Log("GameLogic initialized");
    }

    public override void OnPlayerJoined(VRCPlayerApi player) {
        // try {
        //     // Increase player speed
        //     player.SetWalkSpeed(5);
        //     player.SetRunSpeed(10);
        //     // Increase player jump height
        //     player.SetJumpImpulse(5);
        // } catch (System.Exception e) {
        //     // Could occur if player is remote
        //     Debug.LogError("Game Logic: Error setting player speed: " + e.Message);
        // }
        Debug.Log("Player joined: " + player.displayName);
        if (playerCollider == null) {
            Debug.LogError("Game Logic: Player Collider is not set");
            return;
        } else if (player == null) {
            Debug.LogError("Game Logic: Interacting player is not set");
            return;
        } else if (shuriken == null) {
            Debug.LogError("Game Logic: Shuriken is not set");
            return;
        }
        // Spawn the object
        GameObject spawnedObject = Object.Instantiate(playerCollider);
        spawnedObject.SetActive(true);
        PlayerCollider playerColliderComponent = spawnedObject.GetComponent<PlayerCollider>();
        playerColliderComponent.FollowPlayer(player);
        spawnedObject.transform.position = player.GetPosition();
        Debug.Log("PlayerCollider spawned for player: " + player.displayName);
        
        // Create a shuriken for them
        GameObject shurikenObject = Object.Instantiate(shuriken);
        shurikenObject.SetActive(true);
        Shuriken shurikenComponent = shurikenObject.GetComponent<Shuriken>();
        shurikenComponent.SetOwner(player);
        shurikenComponent.ReturnToOwner();
    }
}
