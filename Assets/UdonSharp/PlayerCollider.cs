using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using Miner28.UdonUtils.Network;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PlayerCollider : NetworkInterface {
    private readonly Vector3 offset = new Vector3(0, 1, 0);
    [UdonSynced] private int playerId = -1;
    [UdonSynced] private int playerNumber = -1;
    [UdonSynced] private bool isAlive = true;

    private Vector3 deathPoint = new Vector3(0, 0, 0);

    public VRCPlayerApi Player {
        get {
            if (playerId == -1) {
                return null;
            }
            return VRCPlayerApi.GetPlayerById(playerId);
        }
    }

    private void Log(string message) {
        Debug.Log("[PlayerCollider - " + playerId + "]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[PlayerCollider - " + playerId + "]: " + message);
    }

    /** Udon Overrides **/

    void Update() {
        if (Player != null) {
            transform.position = Player.GetPosition() + offset;
        }
    }

    public override void OnDeserialization() {
        // Log("Deserializing collider with owner id " + playerId);
        // Log("Is alive: " + isAlive);
        UpdateOwnership();
    }

    /** Event Handlers **/

    [NetworkedMethod]
    public void OnRoundStart(int level) {
        isAlive = true;
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        Log("Next round, resetting player collider");
        GoToLevelSpawn((Level) level);
    }

    [NetworkedMethod]
    public void OnRoundEnd() {
        isAlive = true;
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        Log("Round over, resetting player collider");
        LocalPlayerLogic.Get().ShowScoreUI();
    }

    [NetworkedMethod]
    public void OnGameEnd(int winnerNumber, string winnerName) {
        isAlive = true;
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        Log("Game over, resetting player collider");
        LocalPlayerLogic.Get().ShowGameOverUI(winnerNumber, winnerName);
        GoToLevelSpawn(Level.LOBBY);
    }

    [NetworkedMethod]
    public void OnHit(string playerName, int playerNumber) {
        isAlive = false;
        if (!Networking.IsOwner(gameObject)) {
            Log("Not the owner, skipping collision");
            return;
        }
        Log("Player hit by " + playerName);
        LocalPlayerLogic playerLogic = LocalPlayerLogic.Get();
        playerLogic.ShowHitUI(playerNumber, playerName, "sliced");
        if (playerLogic.GetAlivePlayerCount() > 1) {
            // Only teleport player if this isn't the end of the round
            Player.TeleportTo(deathPoint, Player.GetRotation());
        }
    }

    /** Custom Methods **/

    public void SetPlayerId(int playerId) {
        if (!Networking.IsOwner(gameObject)) {
            LogError("Attempted to set player id without ownership");
            return;
        }
        this.playerId = playerId;
        if (Player == null) {
            LogError("Player Collider: Attempted to follow a null player with id: " + playerId);
            this.playerId = -1;
            return;
        }
        Log("Following player: " + Player.displayName);
        UpdateOwnership();
    }

    public void SetPlayerNumber(int playerNumber) {
        if (!Networking.IsOwner(gameObject)) {
            LogError("Attempted to set player number without ownership");
            return;
        }
        Log("Setting player number to " + playerNumber);
        this.playerNumber = playerNumber;
    }

    public string GetPlayerName() {
        if (Player == null) {
            return "[No player]";
        }
        return Player.displayName;
    }

    public int GetPlayerId() {
        return playerId;
    }

    public bool IsAlive() {
        return isAlive;
    }

    private void GoToLevelSpawn(Level level) {
        LevelManager manager = LevelManager.Get();
        // Get spawn point
        Vector3 spawnPoint = manager.GetSpawnPosition(level, playerNumber);
        // Update world spawn
        LocalPlayerLogic.Get().SetWorldSpawn(spawnPoint);
        // Teleport player to spawn point
        Player.TeleportTo(spawnPoint, Player.GetRotation());
        // Update death point
        UpdateDeathPoint();
    }

    private void UpdateOwnership() {
        if (playerId == Networking.LocalPlayer.playerId && !Networking.IsOwner(gameObject)) {
            Log("Claiming network ownership of collider");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
    }

    private void UpdateDeathPoint() {
        LevelManager manager = LevelManager.Get();
        deathPoint = manager.GetDeathPosition();
    }
}
