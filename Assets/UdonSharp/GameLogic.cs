
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Miner28.UdonUtils.Network;

public class GameLogic : UdonSharpBehaviour {

    /** Used by instance owner to set up the game **/
    public VRC.SDK3.Components.VRCObjectPool shurikenPool;
    public VRC.SDK3.Components.VRCObjectPool playerColliderPool;
    public VRC.SDK3.Components.VRCObjectPool powerUpPool;
    // Used for iterating over the shurikens
    public GameObject shurikensParent;
    public GameObject playerCollidersParent;

    /** Constants **/
    private const float POWER_UP_DELAY = 5000;

    /** Local variables only used by the instance owner **/
    /// <summary>
    /// The current number of players
    /// </summary>
    private int numberOfPlayers = 0;
    /// <summary>
    /// Player names indexed by player number (not player ID)
    /// </summary>
    private readonly string[] playerNames = new string[Shared.MaxPlayers() + 1]; 
    /// <summary>
    /// Player scores indexed by player number (not player ID)
    /// </summary>
    private readonly int[] playerScores = new int[Shared.MaxPlayers() + 1];
    private float nextRoundTime = 0;
    private float nextPowerUpTime = 0;


    /** Synced variables listened to by all players **/
    /// <summary>
    /// The current level (cast to an int for syncing)
    /// </summary>
    [UdonSynced] private int currentLevel = (int) Level.LOBBY;

    
    private void Log(string message) {
        Debug.Log("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    private void LogError(string message) {
        LogError("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    /// <summary>
    /// Get the GameLogic in the scene (there should only be one)
    /// </summary>
    public static GameLogic GetGameLogic() {
        return GameObject.Find("Game Logic").GetComponent<GameLogic>();
    }

    public PlayerCollider GetLocalPlayerCollider(int playerId) {
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null && child.gameObject.GetComponent<Shuriken>().GetPlayerId() == playerId && Networking.IsOwner(child.gameObject)) {
                return child.gameObject.GetComponent<PlayerCollider>();
            }
        }
        LogError("Could not find player collider for local player " + playerId);
        return null;
    }

    public int GetAlivePlayerCount() {
        int count = 0;
        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null && child.gameObject.GetComponent<PlayerCollider>().IsAlive()) {
                count++;
            }
        }
        return count;
    }

    void Start() {
        Log("GameLogic initializing...");
        if (!Networking.IsOwner(gameObject)) {
            Log("Not the owner, skipping rest of initialization");
            return;
        }
        LoadCurrentLevel();
    }

    public override void OnDeserialization() {
        Log("Current level: " + currentLevel);
        LoadCurrentLevel();
    }

    void Update() {
        /** Logic for all players **/
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                if (shuriken.GetPlayerNumber() != -1) {
                    playerScores[shuriken.GetPlayerNumber()] = shuriken.GetScore();
                    if (Networking.GetOwner(child.gameObject) != null) {
                        // Check needed for Unity emulator, otherwise unnecessary
                        playerNames[shuriken.GetPlayerNumber()] = Networking.GetOwner(child.gameObject).displayName;                        
                    }
                }
            }
        }

        /** Logic for instance owner **/
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        int numAlive = GetAlivePlayerCount();
        if (numAlive <= 1 && numberOfPlayers > 1) {
            EndRound();
        }
        if (nextRoundTime != 0 && Time.time * 1000 >= nextRoundTime) {
            StartNextRound();
        }
        if (nextPowerUpTime != 0 && Time.time * 1000 >= nextPowerUpTime) {
            nextPowerUpTime = 0;
            SpawnPowerUp();
        }
    }

    private void EndRound() {
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        nextRoundTime = 0;
        // Send an event to each shuriken
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                shuriken.SendMethodNetworked(nameof(Shuriken.OnRoundOver), SyncTarget.All);
            }
        }
        // Send an event to each player collider
        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null) {
                PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
                playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnRoundOver), SyncTarget.All);
            }
        }
        nextRoundTime = (Time.time + 3) * 1000;
    }

    private void StartNextRound() {
        nextRoundTime = 0;
        if (currentLevel == (int) Level.LOBBY) {
            SwitchLevel(Level.ISLAND_ONE);
        } else {
            SwitchLevel(Level.LOBBY);
        }

        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null) {
                PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
                playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnRoundStart), SyncTarget.All, currentLevel);
            }
        }
    }

    private void SwitchLevel(Level level) {
        if (!Networking.IsOwner(gameObject)) {
            LogError("Non-owner is trying to switch the level, should not happen");
            return;
        }
        // Deactivate all power ups
        foreach (GameObject child in powerUpPool.Pool) {
            powerUpPool.Return(child);
        }
        // Reset the power up timer
        nextPowerUpTime = (Time.time * 1000) + POWER_UP_DELAY;
        // Switch the level
        currentLevel = (int) level;
        LoadCurrentLevel();
    }

    private void LoadCurrentLevel() {
        LevelManager.GetLevelManager().SwitchLevel((Level) currentLevel);
    }

    /// <summary>
    /// Triggered over the network when the game is started
    /// </summary>
    public void StartGame() {
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        Log("Starting game");
        StartNextRound();
    }

    private void SpawnPowerUp() {
        Vector3[] spawnPoints = LevelManager.GetLevelManager().GetPowerUpSpawnPoints((Level) currentLevel);
        if (spawnPoints.Length == 0) {
            LogError("No power up spawn points");
            return;
        }
        GameObject powerUp = powerUpPool.TryToSpawn();
        if (powerUp == null) {
            LogError("Game Logic: No available power ups");
            return;
        }
        powerUp.SetActive(true);
        PowerUp powerUpComponent = powerUp.GetComponent<PowerUp>();
        powerUpComponent.SetRandomPowerUpType();
        powerUp.transform.position = spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    /// <summary>
    /// Triggered locally by the instance owner when a power up is collected
    /// </summary>
    public void OnPowerUpCollected(GameObject powerUp) {
        if (!Networking.IsOwner(gameObject)) {
            LogError("OnPowerUpCollected called by non-owner");
            return;
        }
        powerUpPool.Return(powerUp);
        nextPowerUpTime = (Time.time * 1000) + POWER_UP_DELAY;
    }

    /// <summary>
    /// Triggered by the local player's shuriken when a power up is collected
    /// </summary>
    public void ShowEquippedUI(int powerUpType, int[] powerUps) {
        string currentlyEquipped = "Equipped: ";
        bool start = true;
        for (int i = 0; i < powerUps.Length; i++) {
            if (powerUps[i] != -1) {
                if (!start) {
                    currentlyEquipped += ", ";
                }
                currentlyEquipped += PowerUp.GetPowerUpName(powerUps[i]);
                start = false;
            }
        }

        UIManager.GetUIManager().ShowMessageUI(
            null,
            PowerUp.GetPowerUpName(powerUpType),
            PowerUp.GetPowerUpSubtitle(powerUpType),
            currentlyEquipped,
            false,
            Shared.Colors()[powerUpType % Shared.Colors().Length],
            1200
        );
    }

    public void ShowHitUI(int playerNumber, string senderName, string verb) {
        int numRemaining = GetAlivePlayerCount();
        if (numRemaining <= 1) {
            // Round is over, ignore this UI to wait for round update from instance owner
            return;
        }
        string remaining = "Players Remaining";
        if (numRemaining < 1) {
            remaining = "No " + remaining;
        } else {
            remaining = numRemaining + " " + remaining;
        }
        UIManager.GetUIManager().ShowMessageUI((verb + " by").ToUpper(),
            senderName,
            remaining,
            null,
            true,
            Shared.Colors()[(playerNumber - 1) % Shared.Colors().Length],
            1500);
    }

    public void ShowScoreUI() {
        UIManager.GetUIManager().ShowScoreUI(playerScores, playerNames, 3000);
    }

    public override void OnPlayerJoined(VRCPlayerApi player) {
        Log("Player joined: " + player.displayName);

        if (!Networking.IsOwner(gameObject)) {
            Log("A player joined but we are not the owner so who cares");
            return;
        }

        if (player == null || !Utilities.IsValid(player)) {
            LogError("Somehow, the player is null in OnPlayerJoined");
            return;
        }

        // Increase player speed
        // player.SetWalkSpeed(5);
        // player.SetRunSpeed(8);
        // // Increase player jump height
        // player.SetJumpImpulse(5);

        if (playerColliderPool == null) {
            LogError("Player Collider Pool is not set");
            return;
        } else if (player == null) {
            LogError("Interacting player is not set");
            return;
        } else if (shurikenPool == null) {
            LogError("Shuriken Pool is not set");
            return;
        } else if (powerUpPool == null) {
            LogError("Power Up Pool is not set");
            return;
        }

        if (numberOfPlayers == Shared.MaxPlayers()) {
            LogError("Max players reached, not adding components for " + player.playerId);
            return;
        }

        numberOfPlayers++;

        // Assign a shuriken to the player
        GameObject shuriken = shurikenPool.TryToSpawn();
        if (shuriken == null) {
            LogError("No available shurikens");
            return;
        }
        shuriken.SetActive(true);
        Shuriken shurikenComponent = shuriken.GetComponent<Shuriken>();
        shurikenComponent.SetPlayerId(player.playerId);
        shurikenComponent.SetPlayerNumber(numberOfPlayers);
        shurikenComponent.ReturnToPlayer();


        // Assign a player collider to the player
        GameObject playerCollider = playerColliderPool.TryToSpawn();
        if (playerCollider == null) {
            LogError("No available player colliders");
            return;
        }
        playerCollider.SetActive(true);
        PlayerCollider playerColliderComponent = playerCollider.GetComponent<PlayerCollider>();
        playerColliderComponent.SetPlayerId(player.playerId);
    }
}
