
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Miner28.UdonUtils.Network;
using System;

/// <summary>
/// Game server logic that is only executed by the instance owner (who also owns this object)
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GameLogic : NetworkInterface {

    public VRC.SDK3.Components.VRCObjectPool shurikenPool;
    public VRC.SDK3.Components.VRCObjectPool playerColliderPool;
    public VRC.SDK3.Components.VRCObjectPool powerUpPool;
    public GameObject shurikensParent;
    public GameObject playerCollidersParent;

    /// <summary>
    /// The delay between a power up being collected and the next one spawning
    /// </summary>
    private const float POWER_UP_DELAY = 5000;
    private const int MAX_SCORE = 10;

    /** Synced Variables **/

    /// <summary>
    /// The current number of players. Note that the array starts at 0 but the player number will be the index + 1
    /// </summary>
    [UdonSynced] private bool[] _activePlayers = {false, false, false, false, false, false, false, false};
    /// <summary>
    /// The time at which the next round will start
    /// </summary>
    [UdonSynced] private float nextRoundTime = 0;
    /// <summary>
    /// The time at which the next power up will spawn
    /// </summary>
    [UdonSynced] private float nextPowerUpTime = 0;
    /// <summary>
    /// The current level (cast to an int for syncing)
    /// </summary>
    [UdonSynced] private int _currentLevel = (int) Level.LOBBY;
    
    private void Log(string message) {
        Debug.Log("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    private void LogError(string message) {
        Debug.Log("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    /// <summary>
    /// Get the GameLogic in the scene (there should only be one)
    /// </summary>
    public static GameLogic Get() {
        return GameObject.Find("Game Logic").GetComponent<GameLogic>();
    }

    /** Udon Overrides **/

    void Start() {
        Log("GameLogic initializing...");
        if (!Networking.IsOwner(gameObject)) {
            Log("Not the owner, skipping rest of initialization");
            return;
        }
        OnDeserialization();
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

        int availablePlayerSlot = Array.IndexOf(_activePlayers, false) + 1;

        if (availablePlayerSlot == 0) {
            // TODO: Create guest player
            LogError("Max players reached, not adding components for " + player.playerId);
            return;
        }

        SetPlayerActive(availablePlayerSlot, true);

        // Assign a shuriken to the player
        GameObject shuriken = shurikenPool.TryToSpawn();
        if (shuriken == null) {
            LogError("No available shurikens");
            return;
        }
        shuriken.SetActive(true);
        Shuriken shurikenComponent = shuriken.GetComponent<Shuriken>();
        shurikenComponent.SetPlayerId(player.playerId);
        shurikenComponent.SetPlayerNumber(availablePlayerSlot);
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

    void Update() {
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        if (GetAlivePlayerCount() <= 1 && GetPlayerCount() > 1) {
            Shuriken winner = GetWinnerShuriken();
            if (winner == null) {
                EndRound();
            } else {
                EndGame(winner.GetPlayerNumber(), Networking.GetOwner(winner.gameObject).displayName);
            }
        }
        if (nextRoundTime != 0 && Time.time * 1000 >= nextRoundTime) {
            StartNextRound();
        }
        if (nextPowerUpTime != 0 && Time.time * 1000 >= nextPowerUpTime) {
            nextPowerUpTime = 0;
            SpawnPowerUp();
        }
    }

    public override void OnDeserialization() {
        // Called on every player's client besides the owner by default
        // Owner must manually call this
        Log("Deserializing GameLogic");
        LevelManager.Get().TransitionToLevel(GetCurrentLevel());
    }

    /** Event Handlers **/

    /// <summary>`
    /// Triggered over the network when the game is started
    /// </summary>
    [NetworkedMethod]
    public void StartGame() {
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        Log("Starting game");
        StartNextRound();
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

    /** Getters/setters for synced variables **/

    private Level GetCurrentLevel() {
        return (Level) _currentLevel;
    }

    private int GetCurrentLevelInt() {
        // Udon networking is so stupid
        return _currentLevel;
    }

    private void SetCurrentLevel(Level level) {
        Log("Setting current level to " + level);
        _currentLevel = (int) level;
        RequestSerialization();
        if (Networking.IsOwner(gameObject)) {
            // Since the owner doesn't get OnDeserialization, we need to manually call it
            OnDeserialization();
        }
    }

    private bool[] GetActivePlayers() {
        return _activePlayers;
    }

    public bool IsPlayerActive(int playerNumber) {
        if (playerNumber < 1 || playerNumber > _activePlayers.Length) {
            LogError("Invalid player number: " + playerNumber);
        }
        return _activePlayers[playerNumber - 1];
    }

    private void SetActivePlayers(bool[] activePlayers) {
        _activePlayers = activePlayers;
        RequestSerialization();
    }

    private void SetPlayerActive(int playerNumber, bool active) {
        _activePlayers[playerNumber - 1] = active;
        RequestSerialization();
    }

    /** Custom methods **/

    private int GetAlivePlayerCount() {
        int count = 0;
        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null && child.gameObject.GetComponent<PlayerCollider>().IsAlive()) {
                count++;
            }
        }
        return count;
    }

    private int GetPlayerCount() {
        int count = 0;
        for (int i = 0; i < GetActivePlayers().Length; i++) {
            if (GetActivePlayers()[i]) {
                count++;
            }
        }
        return count;
    }

    private Shuriken GetWinnerShuriken() {
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                if (shuriken.GetScore() >= MAX_SCORE) {
                    return shuriken;
                }
            }
        }
        return null;
    }

    private void EndRound() {
        nextRoundTime = 0;
        // Send an event to each shuriken
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                shuriken.SendMethodNetworked(nameof(Shuriken.OnRoundEnd), SyncTarget.All);
            }
        }
        // Send an event to each player collider
        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null) {
                PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
                playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnRoundEnd), SyncTarget.All);
            }
        }
        nextRoundTime = (Time.time + 3) * 1000;
    }

    private void EndGame(int winnerNumber, string winnerName) {
        // Send an event to each shuriken
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                shuriken.SendMethodNetworked(nameof(Shuriken.OnGameEnd), SyncTarget.All);
            }
        }

        ChangeLevel(Level.LOBBY);

        // Send an event to each player collider
        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null) {
                PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
                playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnGameEnd), SyncTarget.All, winnerNumber, winnerName);
            }
        }
        nextRoundTime = 0;
    }

    private void StartNextRound() {
        nextRoundTime = 0;
        ChangeLevel(LevelManager.GetRandomLevel(GetCurrentLevel()));
        // Send an event to each shuriken
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                shuriken.SendMethodNetworked(nameof(Shuriken.OnRoundStart), SyncTarget.All);
            }
        }

        // Send an event to each player collider
        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null) {
                PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
                Log("Sending start round for player collider " + child.gameObject.name);
                playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnRoundStart), SyncTarget.All, GetCurrentLevelInt());
            }
        }
    }

    private void ChangeLevel(Level level) {
        // Deactivate all power ups
        foreach (GameObject child in powerUpPool.Pool) {
            powerUpPool.Return(child);
        }
        // Reset the power up timer
        nextPowerUpTime = (Time.time * 1000) + POWER_UP_DELAY;
        // Switch the level
        SetCurrentLevel(level);
    }

    private void SpawnPowerUp() {
        Vector3[] spawnPoints = LevelManager.Get().GetPowerUpSpawnPoints(GetCurrentLevel());
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
        powerUp.transform.position = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
    }
}
