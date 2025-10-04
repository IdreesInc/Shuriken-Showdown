using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PlayerCollider : UdonSharpBehaviour
{
    private readonly Vector3 OFFSET = new Vector3(0, 1, 0);
    private PlayerStation playerStation;

    private VRCPlayerApi Player
    {
        get
        {
            return Networking.GetOwner(gameObject);
        }
    }

    private int PlayerId
    {
        get
        {
            return Networking.GetOwner(gameObject).playerId;
        }
    }

    private void Log(string message)
    {
        Shared.Log("PlayerCollider", message, Player);
    }

    private void LogError(string message)
    {
        Shared.LogError("PlayerCollider", message, Player);
    }

    /** Udon Overrides **/

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            GameObject[] playerObjects = Networking.GetPlayerObjects(Player);
            foreach (GameObject obj in playerObjects)
            {
                PlayerStation station = obj.GetComponent<PlayerStation>();
                if (station != null)
                {
                    playerStation = station;
                    break;
                }
            }
            if (playerStation == null)
            {
                LogError("No PlayerStation found for player");
                return;
            }
            // Start at the lobby
            LevelManager.Get().TransitionToLevel(Level.LOBBY);
        }
    }

    void Update()
    {
        if (Player != null)
        {
            transform.position = Player.GetPosition() + OFFSET;
        }
    }

    public override void OnPlayerRespawn(VRCPlayerApi player)
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        if (player.playerId != PlayerId)
        {
            return;
        }
        Log("Player respawned");
        if (!GameLogic.Get().IsPlayerAlive(PlayerId) && GameLogic.Get().GetCurrentLevel() != Level.LOBBY)
        {
            Log("Player tried to cheat death, ghosting them once more");
            GoGhost();
        }
    }

    /** Event Handlers **/

    [NetworkCallable]
    public void OnRoundStart(int level)
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("New round starting on level " + level);
        LevelManager.Get().TransitionToLevel((Level)level);
        GoToLevelSpawn((Level)level);
        // If player is a guest, make them a ghost
        if (!GameLogic.Get().HasPlayerJoined(PlayerId))
        {
            Log("Player is a guest, making them a ghost");
            GoGhost();
        }
    }

    [NetworkCallable]
    public void OnRoundEnd()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Round over, resetting player collider");
        LocalPlayerLogic.Get().ShowScoreUI();
    }

    [NetworkCallable]
    public void OnGameEnd(int winnerNumber, string winnerName)
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Game over, resetting player collider");
        LocalPlayerLogic.Get().ShowGameOverUI(winnerNumber, winnerName);
        GoToLevelSpawn(Level.LOBBY);
    }

    /// <summary>
    /// Called when the player is hit by a shuriken
    /// <param name="playerName">The name of the player who hit this player</param>
    /// <param name="playerSlot">The slot of the player who hit this player</param>
    /// <param name="verb">The verb to use in the hit message</param>
    /// </summary>
    [NetworkCallable]
    public void OnHit(string playerName, int playerSlot, string verb)
    {
        if (!Networking.IsOwner(gameObject))
        {
            Log("Not the owner, skipping collision");
            return;
        }
        Log("Player " + verb + " by " + playerName);
        LocalPlayerLogic playerLogic = LocalPlayerLogic.Get();
        playerLogic.ShowHitUI(playerSlot, playerName, verb);
        GoGhost();
    }

    /// <summary>
    /// Called when the player successfully hits another player
    /// <param name="playerName">The name of the player who was hit</param>
    /// <param name="playerSlot">The slot of the player who was hit</param>
    /// <param name="verb">The verb to use in the kill message</param>
    /// </summary>
    [NetworkCallable]
    public void OnKill(string playerName, int playerSlot, string verb)
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Successfully " + verb + " " + playerName);
        LocalPlayerLogic playerLogic = LocalPlayerLogic.Get();
        playerLogic.ShowKillUI(playerSlot, playerName, verb);
    }

    /** Custom Methods **/

    public string GetPlayerName()
    {
        if (Player == null)
        {
            return "[No player]";
        }
        return Player.displayName;
    }

    public void GoToLevelSpawn(Level level)
    {
        Log("Teleporting to spawn of level " + level);
        LevelManager manager = LevelManager.Get();
        // Get spawn point
        Vector3 spawnPoint = manager.GetSpawnPosition(level, GameLogic.Get().GetPlayerSlot(PlayerId));
        // Update world spawn
        LocalPlayerLogic.Get().SetWorldSpawn(spawnPoint);
        // Teleport player to spawn point (also forces the player out of the station)
        Player.TeleportTo(spawnPoint, Player.GetRotation());
        // Reset station position
        playerStation.ResetLocation();
    }

    private void GoGhost()
    {
        Log("Going ghost!");
        playerStation.MoveToPosition(Player.GetPosition());
        playerStation.SeatPlayer();
    }
}
