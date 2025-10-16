using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
using System;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDK3.Components;

public enum GameState
{
    Lobby,
    RoundStarting,
    Fighting,
    RoundEnding,
    GameEnding
}

/// <summary>
/// Game server logic that is only executed by the instance owner (who also owns this object)
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class GameLogic : UdonSharpBehaviour
{
    public HUD hud;
    public MainMenu mainMenu;
    public GameObject playerObjectsParent;

    public VRCObjectPool powerUpPool;

    /// <summary>
    /// Max number of players that can participate in the game at once
    /// </summary>
    public const int MAX_PLAYERS = 8;
    public const int STARTING_LIVES = 2;
    private const int MAX_MAX_SCORE = 15;
    /// <summary>
    /// The delay between a power up being collected and the next one spawning
    /// </summary>
    private const float POWER_UP_DELAY = 5000;
    /// <summary>
    /// The delay between the start of a round and the start of the fighting
    /// </summary>
    private const float FIGHTING_DELAY = 4000;
    /// <summary>
    /// The delay between the last kill and the end of the round (gives time for the last kill to register on players' UIs)
    /// </summary>
    private const float END_ROUND_DELAY = 500;
    /// <summary>
    /// The delay between the end of a round and the start of the next round
    /// </summary>
    private const float NEXT_ROUND_DELAY = 4500;
    /// <summary>
    /// The delay between the end of the game and returning to the lobby
    /// </summary>
    private const float END_GAME_DELAY = 4000;

    /** Synced Variables **/

    /// <summary>
    /// The score a player needs to reach to win the game
    /// </summary>
    [UdonSynced] private int maxScore = 10;
    /// <summary>
    /// The time limit for each round in seconds
    /// </summary>
    [UdonSynced] private int roundTimeLimit = 120;
    /// <summary>
    /// The current game state
    /// </summary>
    [UdonSynced] private int _currentGameState = (int)GameState.Lobby;
    /// <summary>
    /// The time at which the current state will transition to the next state
    /// </summary>
    [UdonSynced] private float nextStateTime = 0;
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
    // [UdonSynced] private readonly bool[] playerAlive = new bool[MAX_PLAYERS];
    /// <summary>
    /// The number of lives each player has, indexed by player slot
    /// </summary>
    [UdonSynced] private readonly int[] playerLives = new int[MAX_PLAYERS];
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
        Shared.Log("GameLogic", message);
    }

    private void LogError(string message)
    {
        Shared.LogError("GameLogic", message);
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
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        if (playerObjectsParent == null)
        {
            LogError("Player Objects Parent is not set");
            return;
        }
        else if (powerUpPool == null)
        {
            LogError("Power Up Pool is not set");
            return;
        }
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
            LogError("Player is invalid on join");
            return;
        }

        int slot = AddPlayer(player);
        if (slot == -1)
        {
            LogError("Max players reached, not adding player " + player.playerId);
        }
    }

    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        Log("Player left: " + player.displayName);
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }

        RemovePlayer(player);
        CheckForGameEnd();
    }

    void Update()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }

        // Handle state transitions
        if (nextStateTime != 0 && Time.time * 1000 >= nextStateTime)
        {
            nextStateTime = 0;
            GoToNextState();
        }

        // Handle power up spawning
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
        // Update the local HUD UI
        if (HasPlayerJoined(Networking.LocalPlayer.playerId))
        {
            hud.SetScore(playerScores[GetPlayerSlot(Networking.LocalPlayer.playerId)]);
            hud.SetPlayerCount(GetAlivePlayerCount(), GetPlayerCount());
        }
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
        TransitionToRoundStarting();
    }

    [NetworkCallable]
    public void RequestJoin()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Player " + NetworkCalling.CallingPlayer.displayName + " is requesting to join the game");
        if (GetPlayerCount() >= MAX_PLAYERS)
        {
            Log("Player " + NetworkCalling.CallingPlayer.displayName + " tried to join but the game is full");
            return;
        }
        if (HasPlayerJoined(NetworkCalling.CallingPlayer.playerId))
        {
            Log("Player " + NetworkCalling.CallingPlayer.displayName + " is already in the game");
            return;
        }
        AddPlayer(NetworkCalling.CallingPlayer);
    }

    [NetworkCallable]
    public void RequestLeave()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Player " + NetworkCalling.CallingPlayer.displayName + " is requesting to leave the game");
        RemovePlayer(NetworkCalling.CallingPlayer);
        CheckForGameEnd();
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
        int hitPlayerSlot = GetPlayerSlot(hitPlayerId);
        int throwerSlot = GetPlayerSlot(thrower.playerId);

        Log("Player " + thrower.displayName + " " + verb + " player " + VRCPlayerApi.GetPlayerById(hitPlayerId).displayName + ", lives before hit: " + playerLives[hitPlayerSlot]);

        if (hitPlayerSlot < 0)
        {
            LogError("Hit player with id " + hitPlayerId + " not found in any slot");
            return;
        }
        else if (playerLives[hitPlayerSlot] <= 0)
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
        playerLives[hitPlayerSlot]--;
        bool playerKilled = playerLives[hitPlayerSlot] == 0;

        if (playerKilled)
        {
            playerScores[throwerSlot]++;
            Log("Player " + VRCPlayerApi.GetPlayerById(hitPlayerId).displayName + " was killed by " + thrower.displayName + ", new score for " + thrower.displayName + ": " + playerScores[throwerSlot]);
        }

        // Commit the changes
        CommitChanges();

        PlayerCollider[] playerColliders = PlayerColliders();
        // Notify player colliders of the hit so they can play the effects
        foreach (PlayerCollider child in playerColliders)
        {
            if (Networking.GetOwner(child.gameObject).playerId == hitPlayerId)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnHit), playerLives[hitPlayerSlot], thrower.displayName, throwerSlot, verb);
                break;
            }
        }

        if (playerKilled)
        {
            // Notify the thrower of the kill
            foreach (PlayerCollider child in playerColliders)
            {
                if (Networking.GetOwner(child.gameObject).playerId == thrower.playerId)
                {
                    child.SendCustomNetworkEvent(NetworkEventTarget.Owner, nameof(PlayerCollider.OnKill), VRCPlayerApi.GetPlayerById(hitPlayerId).displayName, hitPlayerSlot, verb);
                    break;
                }
            }

            CheckForGameEnd();
        }

        Log("Player hit processing complete");
    }

    /** Getters/setters for synced variables **/

    public int GetMaxScore()
    {
        return maxScore;
    }

    public void ModifyMaxScore(int mod)
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("ModifyMaxScore called by non-owner");
            return;
        }

        maxScore = Mathf.Clamp(maxScore + mod, 1, MAX_MAX_SCORE);

        // Commit the changes
        CommitChanges();
    }

    public int GetRoundTimeLimit()
    {
        return roundTimeLimit;
    }

    public void ModifyRoundTimeLimit(int mod)
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("ModifyRoundTimeLimit called by non-owner");
            return;
        }

        roundTimeLimit = Mathf.Clamp(roundTimeLimit + mod, 15, 600);

        // Commit the changes
        CommitChanges();
    }

    public Level GetCurrentLevel()
    {
        return (Level)_currentLevel;
    }

    public GameState GetCurrentGameState()
    {
        return (GameState)_currentGameState;
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

    private void SetGameState(GameState gameState, float duration = 0)
    {
        _currentGameState = (int)gameState;
        Log("Setting game state to " + GetCurrentGameState().ToString() + (duration > 0 ? " with duration " + duration + "ms" : " indefinitely"));
        if (duration > 0)
        {
            ScheduleNextState(duration);
        }
        else
        {
            nextStateTime = 0;
        }
        CommitChanges();
    }

    private void ScheduleNextState(float duration)
    {
        nextStateTime = (Time.time * 1000) + duration;
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
    /// Check if a player with the given VRChat player ID is playing
    /// Returns false if the player is a guest
    /// </summary>
    public bool HasPlayerJoined(int playerId)
    {
        int playerSlot = GetPlayerSlot(playerId);
        return playerSlot != -1;
    }

    /// <summary>
    /// Check if a player with the given VRChat player ID is alive
    /// Returns false if the player is dead or the player ID is a guest
    /// </summary>
    public bool IsPlayerAlive(int playerId)
    {
        int playerSlot = GetPlayerSlot(playerId);
        return playerSlot >= 0 && playerLives[playerSlot] > 0;
    }

    public int GetAlivePlayerCount()
    {
        int count = 0;
        for (int i = 0; i < playerLives.Length; i++)
        {
            if (playerSlots[i] != 0 && playerLives[i] > 0)
            {
                count++;
            }
        }
        return count;
    }

    public int GetJoinedPlayerSlotCount()
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

    public int[] GetPlayerLives()
    {
        return playerLives;
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

    private void GoToNextState()
    {
        GameState currentState = GetCurrentGameState();

        switch (currentState)
        {
            case GameState.RoundStarting:
                TransitionToFighting();
                break;

            case GameState.Fighting:
                int winnerSlot = GetWinner();
                if (winnerSlot == -1)
                {
                    TransitionToRoundEnding();
                }
                else
                {
                    int winnerId = playerSlots[GetPlayerSlot(winnerSlot)];
                    TransitionToGameEnding(winnerSlot, VRCPlayerApi.GetPlayerById(winnerId).displayName);
                }
                break;

            case GameState.RoundEnding:
                TransitionToRoundStarting();
                break;

            case GameState.GameEnding:
                TransitionToLobby();
                break;
            default:
                LogError("Tried to go to next state from invalid state " + currentState);
                break;
        }
    }

    private void TransitionToLobby()
    {
        SetGameState(GameState.Lobby);

        // Reset player lives
        for (int i = 0; i < playerLives.Length; i++)
        {
            playerLives[i] = STARTING_LIVES;
        }

        // Reset scores
        for (int i = 0; i < playerScores.Length; i++)
        {
            playerScores[i] = 0;
        }

        // Commit the changes
        CommitChanges();
    }

    private void TransitionToRoundStarting()
    {
        SetGameState(GameState.RoundStarting, FIGHTING_DELAY);
        ChangeLevel(LevelManager.GetRandomLevel(GetCurrentLevel()));

        // Reset player lives
        for (int i = 0; i < playerLives.Length; i++)
        {
            playerLives[i] = STARTING_LIVES;
        }

        // Commit the changes
        CommitChanges();

        // Send round start events
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnRoundStart));
            }
        }
        foreach (PlayerCollider child in PlayerColliders())
        {
            if (child.gameObject.activeSelf)
            {
                Log("Sending start round for player collider " + child.gameObject.name);
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnRoundStart), GetCurrentLevelInt());
            }
        }
    }

    private void TransitionToFighting()
    {
        SetGameState(GameState.Fighting, roundTimeLimit * 1000);

        // Send fighting start events
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnFightingStart));
            }
        }
    }

    private void TransitionToRoundEnding()
    {
        SetGameState(GameState.RoundEnding, NEXT_ROUND_DELAY);

        // Send round end events
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnRoundEnd));
            }
        }
        foreach (PlayerCollider child in PlayerColliders())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnRoundEnd));
            }
        }
    }

    private void TransitionToGameEnding(int winnerSlot, string winnerName)
    {
        SetGameState(GameState.GameEnding, END_GAME_DELAY);

        // Reset main menu timer
        mainMenu.ResetTimer();

        // Send game end events
        foreach (Shuriken child in Shurikens())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(Shuriken.OnGameEnd));
            }
        }
        foreach (PlayerCollider child in PlayerColliders())
        {
            if (child.gameObject.activeSelf)
            {
                child.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(PlayerCollider.OnGameEnd), winnerSlot, winnerName);
            }
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
        playerLives[availablePlayerSlot] = STARTING_LIVES;
        playerScores[availablePlayerSlot] = 0;
        Log("Added player " + playerId + " to slot " + availablePlayerSlot);

        // Commit the changes
        CommitChanges();
        return availablePlayerSlot;
    }

    private void RemovePlayer(VRCPlayerApi player)
    {
        int playerId = player.playerId;
        int playerSlot = GetPlayerSlot(playerId);
        if (playerSlot == -1)
        {
            LogError("Player is already guest, no need to remove: " + playerId);
            return;
        }
        playerSlots[playerSlot] = 0;
        playerLives[playerSlot] = 0;
        playerScores[playerSlot] = 0;
        Log("Removed player " + playerId + " from slot " + playerSlot);

        // Commit the changes
        CommitChanges();
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
            if (playerScores[i] >= maxScore)
            {
                return playerSlots[i];
            }
        }
        return -1;
    }

    private void CheckForGameEnd()
    {
        if (GetCurrentGameState() == GameState.Fighting && ((GetAlivePlayerCount() <= 1 && GetPlayerCount() > 1) || GetWinner() != -1))
        {
            ScheduleNextState(END_ROUND_DELAY);
            CommitChanges();
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
