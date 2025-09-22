using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PlayerCollider : UdonSharpBehaviour
{
    private readonly Vector3 offset = new Vector3(0, 1, 0);

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
        Debug.Log("[PlayerCollider - " + PlayerId + "]: " + message);
    }

    private void LogError(string message)
    {
        Debug.LogError("[PlayerCollider - " + PlayerId + "]: " + message);
    }

    /** Udon Overrides **/

    void Update()
    {
        if (Player != null)
        {
            transform.position = Player.GetPosition() + offset;
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
        if (GameLogic.Get().GetAlivePlayerCount() > 1)
        {
            Vector3 deathPoint = LevelManager.Get().GetDeathPosition();
            Player.TeleportTo(deathPoint, Player.GetRotation());
        }
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

    private void GoToLevelSpawn(Level level)
    {
        Log("Teleporting to spawn of level " + level);
        LevelManager manager = LevelManager.Get();
        // Get spawn point
        Vector3 spawnPoint = manager.GetSpawnPosition(level, GameLogic.Get().GetPlayerSlot(PlayerId));
        // Update world spawn
        LocalPlayerLogic.Get().SetWorldSpawn(spawnPoint);
        // Teleport player to spawn point
        Player.TeleportTo(spawnPoint, Player.GetRotation());
    }
}
