
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameLogic : UdonSharpBehaviour {

    public GameObject playerCollider;
    void Start() {
        Debug.Log("GameLogic initialized");
    }

    public override void OnPlayerJoined(VRCPlayerApi player) {
        // Increase player speed
        player.SetWalkSpeed(5);
        player.SetRunSpeed(10);
        // Increase player jump height
        player.SetJumpImpulse(5);
        Debug.Log("Player joined: " + player.displayName);
        if (playerCollider == null) {
            Debug.LogError("Game Logic: Player Collider is not set");
            return;
        } else if (playerCollider == null) {
            Debug.LogError("Game Logic: Interacting player is not set");
            return;
        }
        // Spawn the object
        GameObject spawnedObject = Object.Instantiate(playerCollider);
        spawnedObject.SetActive(true);
        PlayerCollider playerColliderComponent = spawnedObject.GetComponent<PlayerCollider>();
        playerColliderComponent.FollowPlayer(player);
        spawnedObject.transform.position = player.GetPosition();
        Debug.Log("PlayerCollider spawned for player: " + player.displayName);
    }
}
