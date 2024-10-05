using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Miner28.UdonUtils.Network;

public class PlayerCollider : NetworkInterface {
    private readonly Vector3 offset = new Vector3(0, 1, 0);
    [UdonSynced] private int playerId = -1;

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

    [NetworkedMethod]
    public void OnHit(int senderId) {
        if (Networking.LocalPlayer.playerId != playerId) {
            return;
        }
        VRCPlayerApi sender = VRCPlayerApi.GetPlayerById(senderId);
        if (sender == null) {
            LogError("Player hit by unknown player with id: " + senderId + ", ignoring");
            return;
        }
        string senderName = sender.displayName;
        Log("Player hit by " + senderName);
        GameLogic.GetLocalGameLogic().OnHit(senderName, "sliced");
    }

    void Update() {
        if (Player != null) {
            transform.position = Player.GetPosition() + offset;
        }
    }
}
