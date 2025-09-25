using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using System;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDK3.Components;

/// <summary>
/// Game server logic that is only executed by the instance owner (who also owns this object)
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GameLogic : UdonSharpBehaviour
{
    public GameObject playerObjectsParent;

    public VRCObjectPool powerUpPool;

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
    /// <summary>
    /// The delay between the last kill and the end of the round (gives time for the last kill to register on players' UIs)
    /// </summary>
    private const float END_ROUND_DELAY = 500;

    /// <summary>
    /// The delay between the end of a round and the start of the next round
    /// </summary>
    private const float NEXT_ROUND_DELAY = 3000;

    /** Synced Variables **/

    /// <summary>
    /// The time at which the current round will end
    /// </summary>
    [UdonSynced] private float roundEndTime = 0;
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


        int slot = AddPlayer(player);
        if (slot == -1)
        {
            LogError("Max players reached, not adding player " + player.playerId);
            return;
        }
    }

    void Update()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        // Check for timed events
        if (roundEndTime != 0 && Time.time * 1000 >= roundEndTime)
        {
            roundEndTime = 0;
            int winnerSlot = GetWinner();
            if (winnerSlot == -1)
            {
                EndRound();
            }
            else
            {
                int winnerId = playerSlots[GetPlayerSlot(winnerSlot)];
                EndGame(winnerSlot, VRCPlayerApi.GetPlayerById(winnerId).displayName);
            }
        }
        if (nextRoundTime != 0 && Time.time * 1000 >= nextRoundTime)
        {
            StartNextRound();
        }
        if (fightingStartTime != 0 && Time.time * 1000 >= fightingStartTime)
        {
            fightingStartTime = 0;
            // Send an event to each shuriken
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
        // Notify all shurikens of any changes on the client side
        foreach (Shuriken child in Shurikens())
        {
            child.OnGameLogicChange();
        }
        // Update the local scoreboard UI
        LocalUIManager.Get().TriggerScoreboardUpdate();
    }

    /** Event Handlers **/

    /// <summary>
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

        // Commit the changes
        CommitChanges();
    }

    [NetworkCallable]
    public void OnPlayerHit(int hitPlayerId, string verb)
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        VRCPlayerApi thrower = NetworkCalling.CallingPlayer;
        Log("Player " + thrower.displayName + " " + verb + " player " + VRCPlayerApi.GetPlayerById(hitPlayerId).displayName);

        int hitPlayerSlot = GetPlayerSlot(hitPlayerId);
        int throwerSlot = GetPlayerSlot(thrower.playerId);

        if (hitPlayerSlot < 0)
        {
            LogError("Hit player with id " + hitPlayerId + " not found in any slot");
            return;
        }
        else if (!playerAlive[hitPlayerSlot])
        {
            LogError("Hit player in slot " + hitPlayerSlot + " is already dead");
            return;
        }
        if (throwerSlot < 0)
        {
            LogError("Thrower with id " + thrower.playerId + " not found in any slot");
            return;
        }


        // Update player values
        playerScores[throwerSlot]++;
        playerAlive[hitPlayerSlot] = false;

        // Commit the changes
        CommitChanges();

        PlayerCollider[] playerColliders = PlayerColliders();
        // Notify the hit player that they have been hit
        foreach (PlayerCollider child in playerColliders)
        {
            if (Networking.GetOwner(child.gameObject).playerId == hitPlayerId)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PlayerCollider.OnHit), thrower.displayName, throwerSlot, verb);
                break;
            }
        }
        // Notify the thrower of the kill
        foreach (PlayerCollider child in playerColliders)
        {
            if (Networking.GetOwner(child.gameObject).playerId == thrower.playerId)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PlayerCollider.OnKill), VRCPlayerApi.GetPlayerById(hitPlayerId).displayName, hitPlayerSlot, verb);
                break;
            }
        }

        // Check for end of round/game
        if (GetAlivePlayerCount() <= 1 && GetPlayerCount() > 1)
        {
            // Notify players that the round is ending after a short delay
            roundEndTime = (Time.time * 1000) + END_ROUND_DELAY;
        }

        Log("Player hit processing complete");
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
            if (playerSlots[i] != 0 && playerAlive[i])
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

    public int[] GetPlayerSlots()
    {
        return playerSlots;
    }

    public bool[] GetPlayerAliveStatuses()
    {
        return playerAlive;
    }

    private void CommitChanges()
    {
        // Commit the changes
        RequestSerialization();
        // If we are the owner, call OnDeserialization manually (since it won't be called otherwise)
        if (Networking.IsOwner(gameObject))
        {
            OnDeserialization();
        }
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

    private int AddPlayer(VRCPlayerApi player)
    {
        int playerId = player.playerId;
        int availablePlayerSlot = Array.IndexOf(playerSlots, 0);
        if (availablePlayerSlot == -1)
        {
            return -1;
        }
        playerSlots[availablePlayerSlot] = playerId;
        playerAlive[availablePlayerSlot] = true;
        playerScores[availablePlayerSlot] = 0;
        Log("Added player " + playerId + " to slot " + availablePlayerSlot);

        // Commit the changes
        CommitChanges();
        return availablePlayerSlot;
    }

    private Shuriken[] Shurikens()
    {
        return playerObjectsParent.GetComponentsInChildren<Shuriken>();
    }

    private PlayerCollider[] PlayerColliders()
    {
        return playerObjectsParent.GetComponentsInChildren<PlayerCollider>();
    }

    /// <summary>
    /// Returns the winning player slot, or -1 if no winner
    /// </summary>
    private int GetWinner()
    {
        for (int i = 0; i < playerScores.Length; i++)
        {
            if (playerScores[i] >= MAX_SCORE)
            {
                return playerSlots[i];
            }
        }
        return -1;
    }

    private void EndRound()
    {
        Log("Ending round");
        nextRoundTime = Time.time + NEXT_ROUND_DELAY;

        // Reset alive statuses
        for (int i = 0; i < playerAlive.Length; i++)
        {
            playerAlive[i] = true;
        }

        // Commit the changes
        CommitChanges();

        // Send an event to each shuriken
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnRoundEnd));
            }
        }
        // Send an event to each player collider
        foreach (PlayerCollider child in PlayerColliders())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnRoundEnd));
            }
        }
    }

    private void EndGame(int winnerSlot, string winnerName)
    {
        ChangeLevel(Level.LOBBY);
        nextRoundTime = 0;

        // Commit the changes
        CommitChanges();

        // Send an event to each shuriken
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnGameEnd));
            }
        }
        // Send an event to each player collider
        foreach (PlayerCollider child in PlayerColliders())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnGameEnd), winnerSlot, winnerName);
            }
        }
    }

    private void StartNextRound()
    {
        nextRoundTime = 0;
        fightingStartTime = (Time.time * 1000) + FIGHTING_DELAY;
        ChangeLevel(LevelManager.GetRandomLevel(GetCurrentLevel()));

        // Commit the changes
        CommitChanges();

        // Send an event to each shuriken
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                // Intentionally send to all shurikens so they can apply power-ups
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnRoundStart));
            }
        }
        // Send an event to each player collider
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

        // Commit the changes
        CommitChanges();
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
