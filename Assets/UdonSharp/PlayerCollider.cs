using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PlayerCollider : UdonSharpBehaviour
{
    public HUD hud;

    private ParticleSystem shieldPopEffect;

    private const float WATER_LEVEL = -2.8f;
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
        if (hud == null)
        {
            LogError("hud is not set");
        }
        shieldPopEffect = transform.Find("Shield Pop").GetComponent<ParticleSystem>();
        if (shieldPopEffect == null)
        {
            LogError("Shield Pop particle system not found");
        }

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
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        if (GameLogic.Get().IsPlayerAlive(PlayerId))
        {
            if (Player.GetPosition().y < WATER_LEVEL)
            {
                Log("Player touched water, respawning");
                Player.Respawn();
                Log("Player respawned at y level " + Player.GetPosition().y);
            }
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
        // Update HUD
        hud.ResetForNewRound();
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
        hud.ResetHud();
    }

    /// <summary>
    /// Called when the player is hit by a shuriken
    /// <param name="playerName">The name of the player who hit this player</param>
    /// <param name="playerSlot">The slot of the player who hit this player</param>
    /// <param name="verb">The verb to use in the hit message</param>
    /// </summary>
    [NetworkCallable]
    public void OnHit(int livesRemaining, string playerName, int playerSlot, string verb)
    {
        if (livesRemaining == 1)
        {
            Log("Playing shield pop effect");
            shieldPopEffect.Play();
        }
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Player " + verb + " by " + playerName + ", lives remaining: " + livesRemaining);
        hud.SetLives(livesRemaining);
        if (livesRemaining > 0)
        {
            // Player is not yet dead
            return;
        }
        LocalPlayerLogic playerLogic = LocalPlayerLogic.Get();
        playerLogic.ShowHitUI(playerSlot, playerName, verb);
        GoGhost();
    }

    /// <summary>
    /// Called when the player successfully kills another player
    /// <param name="playerName">The name of the player who was killed</param>
    /// <param name="playerSlot">The slot of the player who was killed</param>
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
