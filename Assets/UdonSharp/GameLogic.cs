
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using System;
using VRC.SDK3.UdonNetworkCalling;

/// <summary>
/// Game server logic that is only executed by the instance owner (who also owns this object)
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GameLogic : UdonSharpBehaviour
{

    // public VRC.SDK3.Components.VRCObjectPool shurikenPool;
    // public VRC.SDK3.Components.VRCObjectPool playerColliderPool;

    public GameObject playerObjectsParent;

    public VRC.SDK3.Components.VRCObjectPool powerUpPool;
    // public GameObject shurikensParent;
    // public GameObject playerCollidersParent;

    /// <summary>
    /// Max number of players that can participate in the game at once
    /// </summary>
    public const int MAX_PLAYERS = 8;


    /// <summary>
    /// The delay between a power up being collected and the next one spawning
    /// </summary>
    private const float POWER_UP_DELAY = 5000;
    /// <summary>
    /// The maximum score a player can achieve before the game ends
    /// </summary>
    private const int MAX_SCORE = 10;
    /// <summary>
    /// The delay between the start of a round and the start of the fighting
    /// </summary>
    private const float FIGHTING_DELAY = 3000;

    /** Synced Variables **/

    /// <summary>
    /// The time at which the next round will start
    /// </summary>
    [UdonSynced] private float nextRoundTime = 0;
    /// <summary>
    /// The time at which the fighting will start
    /// </summary>
    [UdonSynced] private float fightingStartTime = 0;
    /// <summary>
    /// The time at which the next power up will spawn
    /// </summary>
    [UdonSynced] private float nextPowerUpTime = 0;
    /// <summary>
    /// Array of players IDs for active players, 0 if the slot is empty
    /// </summary>
    [UdonSynced] private readonly int[] playerSlots = new int[MAX_PLAYERS];
    /// <summary>
    /// Whether each player is alive or not, indexed by player slot
    /// </summary>
    [UdonSynced] private readonly bool[] playerAlive = new bool[MAX_PLAYERS];
    /// <summary>
    /// The scores of each player, indexed by player slot
    /// </summary>
    [UdonSynced] private readonly int[] playerScores = new int[MAX_PLAYERS];

    /// <summary>
    /// The current level (cast to an int for syncing)
    /// TODO: Turn into one of those magic fields to convert between enum and int automatically
    /// </summary>
    [UdonSynced] private int _currentLevel = (int)Level.LOBBY;

    private void Log(string message)
    {
        Debug.Log("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    private void LogError(string message)
    {
        Debug.Log("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    /// <summary>
    /// Get the GameLogic singleton
    /// </summary>
    public static GameLogic Get()
    {
        return GameObject.Find("Game Logic").GetComponent<GameLogic>();
    }

    /** Udon Overrides **/

    void Start()
    {
        Log("GameLogic initializing...");
        if (!Networking.IsOwner(gameObject))
        {
            Log("Not the owner, skipping rest of initialization");
            return;
        }
        OnDeserialization();
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        Log("Player joined: " + player.displayName);
        if (!Networking.IsOwner(gameObject))
        {
            Log("A player joined but we are not the owner so who cares");
            return;
        }

        if (player == null || !Utilities.IsValid(player))
        {
            LogError("Somehow, the player is null in OnPlayerJoined");
            return;
        }

        // Increase player speed
        // player.SetWalkSpeed(5);
        // player.SetRunSpeed(8);
        // // Increase player jump height
        // player.SetJumpImpulse(5);

        if (player == null)
        {
            LogError("Interacting player is not set");
            return;
        }
        else if (powerUpPool == null)
        {
            LogError("Power Up Pool is not set");
            return;
        }


        bool success = AddPlayer(player.playerId);
        if (!success)
        {
            LogError("Max players reached, not adding player " + player.playerId);
            return;
        }

        // // Create a shuriken for the player
        // GameObject shuriken = shurikenPool.TryToSpawn();
        // if (shuriken == null)
        // {
        //     LogError("No available shurikens");
        //     return;
        // }
        // shuriken.SetActive(true);
        // Shuriken shurikenComponent = shuriken.GetComponent<Shuriken>();
        // shurikenComponent.SetPlayerNumber(availablePlayerSlot);
        // // Assign ownership of the shuriken to the player
        // shurikenComponent.SetPlayerId(player.playerId);

        // // Create a player collider for the player
        // GameObject playerCollider = playerColliderPool.TryToSpawn();
        // if (playerCollider == null)
        // {
        //     LogError("No available player colliders");
        //     return;
        // }
        // playerCollider.SetActive(true);
        // PlayerCollider playerColliderComponent = playerCollider.GetComponent<PlayerCollider>();
        // // playerColliderComponent.SetPlayerNumber(availablePlayerSlot);
        // // Assign ownership of the player collider to the player
        // // playerColliderComponent.SetPlayerId(player.playerId);
        // Networking.SetOwner(player, playerCollider);
    }

    void Update()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        if (GetAlivePlayerCount() <= 1 && GetPlayerCount() > 1)
        {
            // Shuriken winner = GetWinnerShuriken();
            // if (winner == null) {
            EndRound();
            // } else {
            //     EndGame(winner.GetPlayerNumber(), Networking.GetOwner(winner.gameObject).displayName);
            // }
        }
        if (nextRoundTime != 0 && Time.time * 1000 >= nextRoundTime)
        {
            StartNextRound();
        }
        if (fightingStartTime != 0 && Time.time * 1000 >= fightingStartTime)
        {
            fightingStartTime = 0;
            // Send an event to each shuriken
            // foreach (Transform child in shurikensParent.transform) {
            //     if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null)
            //     {
            //         Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
            //         // shuriken.SendMethodNetworked(nameof(Shuriken.OnFightingStart), SyncTarget.All);
            //         SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnFightingStart));
            //     }
            // }
            // SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnFightingStart));
            foreach (Shuriken child in Shurikens())
            {
                if (child.gameObject.activeSelf)
                {
                    child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnFightingStart));
                }
            }
        }
        if (nextPowerUpTime != 0 && Time.time * 1000 >= nextPowerUpTime)
        {
            nextPowerUpTime = 0;
            SpawnPowerUp();
        }
    }

    public override void OnDeserialization()
    {
        // Called on every player's client besides the owner by default
        // Owner must manually call this
        Log("Deserializing GameLogic");
        LevelManager.Get().TransitionToLevel(GetCurrentLevel());
    }

    /** Event Handlers **/

    /// <summary>`
    /// Triggered over the network when the game is started
    /// </summary>
    [NetworkCallable]
    public void StartGame()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Starting game");
        StartNextRound();
    }

    /// <summary>
    /// Triggered locally by the instance owner when a power up is collected
    /// </summary>
    public void OnPowerUpCollected(GameObject powerUp)
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("OnPowerUpCollected called by non-owner");
            return;
        }
        powerUpPool.Return(powerUp);
        nextPowerUpTime = (Time.time * 1000) + POWER_UP_DELAY;
    }

    /** Getters/setters for synced variables **/

    private Level GetCurrentLevel()
    {
        return (Level)_currentLevel;
    }

    private int GetCurrentLevelInt()
    {
        // Udon networking is so stupid
        return _currentLevel;
    }

    private void SetCurrentLevel(Level level)
    {
        Log("Setting current level to " + level);
        _currentLevel = (int)level;
        RequestSerialization();
        if (Networking.IsOwner(gameObject))
        {
            // Since the owner doesn't get OnDeserialization, we need to manually call it
            OnDeserialization();
        }
    }

    /** Custom methods **/

    /// <summary>
    /// Get the player slot index for a given VRChat player ID, or -1 if not found
    /// </summary>
    public int GetPlayerSlot(int playerId)
    {
        return Array.IndexOf(playerSlots, playerId);
    }

    /// <summary>
    /// Check if a player with the given VRChat player ID is alive
    /// Returns false if the player is dead or the player ID is not found
    /// </summary>
    public bool IsPlayerAlive(int playerId)
    {
        int playerSlot = GetPlayerSlot(playerId);
        if (playerSlot < 0 || playerSlot >= playerAlive.Length)
        {
            return false;
        }
        return playerAlive[playerSlot];
    }

    public int GetAlivePlayerCount()
    {

        int count = 0;
        for (int i = 0; i < playerAlive.Length; i++)
        {
            if (playerAlive[i])
            {
                count++;
            }
        }
        return count;
    }

    public bool IsPlayerSlotActive(int playerSlot)
    {
        if (playerSlot < 0 || playerSlot >= playerSlots.Length)
        {
            return false;
        }
        return playerSlots[playerSlot] != 0;
    }

    public int[] GetPlayerScores()
    {
        return playerScores;
    }

    private int GetPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != 0)
            {
                count++;
            }
        }
        return count;
    }

    private bool AddPlayer(int playerId)
    {
        int availablePlayerSlot = Array.IndexOf(playerSlots, 0);
        if (availablePlayerSlot == -1)
        {
            return false;
        }
        playerSlots[availablePlayerSlot] = playerId;
        playerAlive[availablePlayerSlot] = true;
        playerScores[availablePlayerSlot] = 0;
        Log("Added player " + playerId + " to slot " + availablePlayerSlot);
        RequestSerialization();
        return true;
    }

    private Shuriken[] Shurikens()
    {
        return playerObjectsParent.GetComponentsInChildren<Shuriken>();
    }

    private PlayerCollider[] PlayerColliders()
    {
        return playerObjectsParent.GetComponentsInChildren<PlayerCollider>();
    }

    // private Shuriken GetWinnerShuriken() {
    //     foreach (Transform child in shurikensParent.transform) {
    //         if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
    //             Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
    //             if (shuriken.GetScore() >= MAX_SCORE) {
    //                 return shuriken;
    //             }
    //         }
    //     }
    //     return null;
    // }

    private void EndRound()
    {
        nextRoundTime = 0;
        // Send an event to each shuriken
        // foreach (Transform child in shurikensParent.transform) {
        //     if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null)
        //     {
        //         Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
        //         // shuriken.SendMethodNetworked(nameof(Shuriken.OnRoundEnd), SyncTarget.All);
        //         SendCustomNetworkEvent(NetworkEventTarget.All, nameof(shuriken.OnRoundEnd));
        //     }
        // }
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnRoundEnd));
            }
        }
        // Send an event to each player collider
        // foreach (Transform child in playerCollidersParent.transform)
        // {
        //     if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null)
        //     {
        //         PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
        //         // playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnRoundEnd), SyncTarget.All);
        //         SendCustomNetworkEvent(NetworkEventTarget.All, nameof(playerCollider.OnRoundEnd));
        //     }
        // }
        foreach (PlayerCollider child in PlayerColliders())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnRoundEnd));
            }
        }
        nextRoundTime = (Time.time + 3) * 1000;
    }

    private void EndGame(int winnerNumber, string winnerName)
    {
        // Send an event to each shuriken
        // foreach (Transform child in shurikensParent.transform) {
        //     if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null)
        //     {
        //         Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
        //         // shuriken.SendMethodNetworked(nameof(Shuriken.OnGameEnd), SyncTarget.All);
        //         SendCustomNetworkEvent(NetworkEventTarget.All, nameof(shuriken.OnGameEnd));
        //     }
        // }
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnGameEnd));
            }
        }
        ChangeLevel(Level.LOBBY);

        // Send an event to each player collider
        // foreach (Transform child in playerCollidersParent.transform) {
        //     if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null) {
        //         PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
        //         // playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnGameEnd), SyncTarget.All, winnerNumber, winnerName);
        //         SendCustomNetworkEvent(NetworkEventTarget.All, nameof(playerCollider.OnGameEnd), winnerNumber, winnerName);
        //     }
        // }
        foreach (PlayerCollider child in PlayerColliders())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnGameEnd), winnerNumber, winnerName);
            }
        }
        nextRoundTime = 0;
    }

    private void StartNextRound()
    {
        nextRoundTime = 0;
        fightingStartTime = (Time.time * 1000) + FIGHTING_DELAY;
        ChangeLevel(LevelManager.GetRandomLevel(GetCurrentLevel()));
        // Send an event to each shuriken
        // foreach (Transform child in shurikensParent.transform) {
        //     if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null)
        //     {
        //         Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
        //         // shuriken.SendMethodNetworked(nameof(Shuriken.OnRoundStart), SyncTarget.All);
        //         SendCustomNetworkEvent(NetworkEventTarget.All, nameof(shuriken.OnRoundStart));
        //     }
        // }
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnRoundStart));
            }
        }

        // Send an event to each player collider
        // foreach (Transform child in playerCollidersParent.transform)
        // {
        //     if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null)
        //     {
        //         PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
        //         Log("Sending start round for player collider " + child.gameObject.name);
        //         // playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnRoundStart), SyncTarget.All, GetCurrentLevelInt());
        //         SendCustomNetworkEvent(NetworkEventTarget.All, nameof(playerCollider.OnRoundStart), GetCurrentLevelInt());
        //     }
        // }

        foreach (PlayerCollider child in PlayerColliders())
        {
            if (child.gameObject.activeSelf)
            {
                Log("Sending start round for player collider " + child.gameObject.name);
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnRoundStart), GetCurrentLevelInt());
            }
        }
    }

    private void ChangeLevel(Level level)
    {
        // Deactivate all power ups
        foreach (GameObject child in powerUpPool.Pool)
        {
            powerUpPool.Return(child);
        }
        // Reset the power up timer
        nextPowerUpTime = (Time.time * 1000) + POWER_UP_DELAY;
        // Switch the level
        SetCurrentLevel(level);
    }

    private void SpawnPowerUp()
    {
        Vector3[] spawnPoints = LevelManager.Get().GetPowerUpSpawnPoints(GetCurrentLevel());
        if (spawnPoints.Length == 0)
        {
            LogError("No power up spawn points");
            return;
        }
        GameObject powerUp = powerUpPool.TryToSpawn();
        if (powerUp == null)
        {
            LogError("Game Logic: No available power ups");
            return;
        }
        powerUp.SetActive(true);
        PowerUp powerUpComponent = powerUp.GetComponent<PowerUp>();
        powerUpComponent.SetRandomPowerUpType();
        powerUp.transform.position = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
    }
}
