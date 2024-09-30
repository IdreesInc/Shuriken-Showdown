
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameLogic : UdonSharpBehaviour {

    public GameObject[] playerColliderPool;
    public GameObject[] shurikenPool;

    private bool[] playerCollidersInUse;
    private bool[] shurikensInUse;

    void Start() {
        Debug.Log("GameLogic initialized");
        playerCollidersInUse = new bool[playerColliderPool.Length];
        shurikensInUse = new bool[shurikenPool.Length];
    }

    private GameObject GetAvailablePlayerCollider() {
        for (int i = 0; i < playerColliderPool.Length; i++) {
            if (!playerCollidersInUse[i]) {
                playerCollidersInUse[i] = true;
                return playerColliderPool[i];
            }
        }
        return null;
    }

    private GameObject GetAvailableShuriken() {
        for (int i = 0; i < shurikenPool.Length; i++) {
            if (!shurikensInUse[i]) {
                shurikensInUse[i] = true;
                return shurikenPool[i];
            }
        }
        return null;
    }

    public override void OnPlayerJoined(VRCPlayerApi player) {
        // Increase player speed
        player.SetWalkSpeed(5);
        player.SetRunSpeed(10);
        // Increase player jump height
        player.SetJumpImpulse(5);
        
        Debug.Log("Player joined: " + player.displayName);
        if (playerColliderPool == null) {
            Debug.LogError("Game Logic: Player Collider Pool is not set");
            return;
        } else if (player == null) {
            Debug.LogError("Game Logic: Interacting player is not set");
            return;
        } else if (shurikenPool == null) {
            Debug.LogError("Game Logic: Shuriken Pool is not set");
        }

        // Assign a player collider to the player
        GameObject playerCollider = GetAvailablePlayerCollider();
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
        GameObject shuriken = GetAvailableShuriken();
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
