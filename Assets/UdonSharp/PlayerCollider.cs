using UdonSharp;
using UnityEngine;
using VRC.SDK3.UdonNetworkCalling;
using VRC.SDKBase;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PlayerCollider : UdonSharpBehaviour
{
    private readonly Vector3 offset = new Vector3(0, 1, 0);
    // [UdonSynced] private int playerId = -1;
    // [UdonSynced] private int playerNumber = -1;
    // [UdonSynced] private bool isAlive = true;

    /// <summary>
    /// Used locally by non-owners to prevent repeated hits before isAlive is updated
    /// </summary>
    // public bool hasBeenHitLocally = false;

    // private Vector3 deathPoint = new Vector3(0, 0, 0);

    private VRCPlayerApi Player
    {
        get
        {
            // if (playerId == -1) {
            //     return null;
            // }
            // return VRCPlayerApi.GetPlayerById(playerId);
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

    public override void OnDeserialization()
    {
        // Log("Deserializing collider with owner id " + playerId);
        // Log("Is alive: " + isAlive);
        // UpdateOwnership();
    }

    /** Event Handlers **/

    [NetworkCallable]
    public void OnRoundStart(int level)
    {
        // isAlive = true;
        // hasBeenHitLocally = false;
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Next round, resetting player collider");
        GoToLevelSpawn((Level)level);
    }

    [NetworkCallable]
    public void OnRoundEnd()
    {
        // isAlive = true;
        // hasBeenHitLocally = false;
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
        // isAlive = true;
        // hasBeenHitLocally = false;
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Game over, resetting player collider");
        LocalPlayerLogic.Get().ShowGameOverUI(winnerNumber, winnerName);
        GoToLevelSpawn(Level.LOBBY);
    }

    [NetworkCallable]
    public void OnHit(string playerName, int playerSlot)
    {
        // isAlive = false;
        if (!Networking.IsOwner(gameObject))
        {
            Log("Not the owner, skipping collision");
            return;
        }
        Log("Player hit by " + playerName);
        LocalPlayerLogic playerLogic = LocalPlayerLogic.Get();
        playerLogic.ShowHitUI(playerSlot, playerName, "sliced");
        if (GameLogic.Get().GetAlivePlayerCount() > 1)
        {
            Vector3 deathPoint = LevelManager.Get().GetDeathPosition();
            Player.TeleportTo(deathPoint, Player.GetRotation());
        }
    }

    /** Custom Methods **/

    // public void SetPlayerId(int playerId) {
    //     if (!Networking.IsOwner(gameObject)) {
    //         LogError("Attempted to set player id without ownership");
    //         return;
    //     }
    //     this.playerId = playerId;
    //     if (Player == null) {
    //         LogError("Player Collider: Attempted to follow a null player with id: " + playerId);
    //         this.playerId = -1;
    //         return;
    //     }
    //     Log("Following player: " + Player.displayName);
    //     UpdateOwnership();
    // }

    // public void SetPlayerNumber(int playerNumber) {
    //     if (!Networking.IsOwner(gameObject)) {
    //         LogError("Attempted to set player number without ownership");
    //         return;
    //     }
    //     Log("Setting player number to " + playerNumber);
    //     this.playerNumber = playerNumber;
    // }

    public string GetPlayerName()
    {
        if (Player == null)
        {
            return "[No player]";
        }
        return Player.displayName;
    }

    // public int GetPlayerId() {
    //     return playerId;
    // }

    // public int GetPlayerNumber() {
    //     return playerNumber;
    // }

    // public bool IsAlive() {
    //     return isAlive;
    // }

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

    // private void UpdateOwnership() {
    //     if (playerId == Networking.LocalPlayer.playerId && !Networking.IsOwner(gameObject)) {
    //         Log("Claiming network ownership of collider");
    //         Networking.SetOwner(Networking.LocalPlayer, gameObject);
    //     }
    // }
}
