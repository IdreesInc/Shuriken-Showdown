using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerCollider : UdonSharpBehaviour {
    [UdonSynced] private int playerId = -1;
    private readonly Vector3 offset = new Vector3(0, 1, 0);

    private void Log(string message) {
        Debug.Log("[PlayerCollider - " + playerId + "]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[PlayerCollider - " + playerId + "]: " + message);
    }

    public VRCPlayerApi Player {
        get {
            if (playerId == -1) {
                return null;
            }
            return VRCPlayerApi.GetPlayerById(playerId);
        }
    }

    public void FollowPlayer(int playerId) {
        this.playerId = playerId;
        if (Player == null) {
            LogError("Player Collider: Attempted to follow a null player with id: " + playerId);
            this.playerId = -1;
            return;
        }
        Log("Following player: " + Player.displayName);
        Networking.SetOwner(Player, gameObject);
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

    void Update() {
        if (Player != null) {
            transform.position = Player.GetPosition() + offset;
        }
    }
}
