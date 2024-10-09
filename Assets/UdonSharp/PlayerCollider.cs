using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Miner28.UdonUtils.Network;

public class PlayerCollider : NetworkInterface {
    private readonly Vector3 offset = new Vector3(0, 1, 0);
    [UdonSynced] private int playerId = -1;
    [UdonSynced] private bool isAlive = true;

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

    public void SetPlayerId(int playerId) {
        this.playerId = playerId;
        if (Player == null) {
            LogError("Player Collider: Attempted to follow a null player with id: " + playerId);
            this.playerId = -1;
            return;
        }
        Log("Following player: " + Player.displayName);
        UpdateOwnership();
    }

    public void UpdateOwnership() {
        if (playerId == Networking.LocalPlayer.playerId && !Networking.IsOwner(gameObject)) {
            Log("Claiming network ownership of collider");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
    }

    public override void OnDeserialization() {
        // Log("Deserializing collider with owner id " + playerId);
        // Log("Is alive: " + isAlive);
        UpdateOwnership();
    }

    public string GetPlayerName() {
        if (Player == null) {
            return "[No player]";
        }
        return Player.displayName;
    }

    public int GetPlayer() {
        return playerId;
    }

    public bool IsAlive() {
        return isAlive;
    }

    private Vector3 GetDeathMarkerLocation() {
        GameObject deathMarker = GameObject.Find("Death Marker");
        if (deathMarker == null) {
            LogError("Death marker not found");
            return Vector3.zero;
        }
        return deathMarker.transform.position;
    }

    [NetworkedMethod]
    public void OnHit(string playerName, int playerNumber) {
        isAlive = false;
        if (!Networking.IsOwner(gameObject)) {
            Log("Not the owner, skipping collision");
            return;
        }
        Log("Player hit by " + playerName);
        // Notify local game logic to update UI
        GameLogic.GetGameLogic().OnHit(playerNumber, playerName, "sliced");
        // Teleport player to death marker
        Player.TeleportTo(GetDeathMarkerLocation(), Player.GetRotation());
    }

    [NetworkedMethod]
    public void OnRoundOver() {
        isAlive = true;
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        Log("Round over, resetting player collider");
        // Notify local game logic to update UI
        GameLogic.GetGameLogic().OnRoundOver();
    }

    void Update() {
        if (Player != null) {
            transform.position = Player.GetPosition() + offset;
        }
    }
}
