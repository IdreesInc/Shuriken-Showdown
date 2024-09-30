
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameLogic : UdonSharpBehaviour {

    public GameObject[] playerColliderPool;
    public GameObject[] shurikenPool;
    public GameObject[] powerUpPool;

    private bool[] playerCollidersInUse;
    private bool[] shurikensInUse;
    private bool[] powerUpsInUse;

    void Start() {
        Debug.Log("GameLogic initialized");
        playerCollidersInUse = new bool[playerColliderPool.Length];
        shurikensInUse = new bool[shurikenPool.Length];
        powerUpsInUse = new bool[powerUpPool.Length];

        // Put a power up at position -1, 3, -0.3
        GameObject powerUp = GetFromPool(powerUpPool, powerUpsInUse);
        if (powerUp == null) {
            Debug.LogError("Game Logic: No available power ups");
            return;
        }
        powerUp.SetActive(true);
        powerUp.transform.position = new Vector3(-1, 2, -0.3f);
    }

    private GameObject GetFromPool(GameObject[] pool, bool[] inUse) {
        for (int i = 0; i < pool.Length; i++) {
            if (!inUse[i]) {
                inUse[i] = true;
                return pool[i];
            }
        }
        return null;
    }

    private GameObject GetAvailablePlayerCollider() {
        return GetFromPool(playerColliderPool, playerCollidersInUse);
    }

    private GameObject GetAvailableShuriken() {
        return GetFromPool(shurikenPool, shurikensInUse);
    }

    public override void OnPlayerJoined(VRCPlayerApi player) {
        // Increase player speed
        player.SetWalkSpeed(5);
        player.SetRunSpeed(8);
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
            return;
        } else if (powerUpPool == null) {
            Debug.LogError("Game Logic: Power Up Pool is not set");
            return;
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
