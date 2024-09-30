
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameLogic : UdonSharpBehaviour {

    public VRC.SDK3.Components.VRCObjectPool shurikenPool;
    public VRC.SDK3.Components.VRCObjectPool playerColliderPool;
    public VRC.SDK3.Components.VRCObjectPool powerUpPool;

    void Start() {
        Debug.Log("GameLogic initialized");

        GameObject powerUp = powerUpPool.TryToSpawn();
        if (powerUp == null) {
            Debug.LogError("Game Logic: No available power ups");
            return;
        }
        powerUp.SetActive(true);
        powerUp.transform.position = new Vector3(-1, 2, -0.3f);
    }

    public override void OnPlayerJoined(VRCPlayerApi player) {
        if (player == null || !Utilities.IsValid(player)) {
            Debug.LogError("Game Logic: Somehow, the player is null in OnPlayerJoined");
            return;
        }

        // Increase player speed
        // player.SetWalkSpeed(5);
        // player.SetRunSpeed(8);
        // // Increase player jump height
        // player.SetJumpImpulse(5);

        Debug.Log("Player joined: " + player.displayName);
        if (playerColliderPool == null) {
            Debug.LogError("Game Logic: Player Collider Pool is not set");
            return;
        } else if (player == null) {
            Debug.LogError("Game Logic: Interacting player is not set");
            return;
        } else if (shurikenPool == null) {
            Debug.LogError("Game Logic: Shuriken Pool is not set");
            return;
        } else if (powerUpPool == null) {
            Debug.LogError("Game Logic: Power Up Pool is not set");
            return;
        }

        // Assign a player collider to the player
        GameObject playerCollider = playerColliderPool.TryToSpawn();
        if (playerCollider == null) {
            Debug.LogError("Game Logic: No available player colliders");
            return;
        }
        playerCollider.SetActive(true);
        PlayerCollider playerColliderComponent = playerCollider.GetComponent<PlayerCollider>();
        playerColliderComponent.FollowPlayer(player);
        playerCollider.transform.position = player.GetPosition();
        Debug.Log("PlayerCollider spawned for player: " + player.displayName);

        // Assign a shuriken to the player
        GameObject shuriken = shurikenPool.TryToSpawn();
        if (shuriken == null) {
            Debug.LogError("Game Logic: No available shurikens");
            return;
        }
        shuriken.SetActive(true);
        Shuriken shurikenComponent = shuriken.GetComponent<Shuriken>();
        shurikenComponent.SetOwner(player);
        shurikenComponent.ReturnToOwner();
    }
}
