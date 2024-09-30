using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerCollider : UdonSharpBehaviour {
    private VRCPlayerApi playerToFollow;
    private Vector3 offset = new Vector3(0, 1, 0);
    public void FollowPlayer(VRCPlayerApi player) {
        if (player == null) {
            Debug.LogError("Player Collider: Player is null");
            return;
        }
        playerToFollow = player;
        Debug.Log("Following player: " + player.displayName);
        Networking.SetOwner(player, gameObject);
    }

    public String GetPlayerName() {
        if (playerToFollow == null) {
            return "[No player]";
        }
        return playerToFollow.displayName;
    }

    public VRCPlayerApi GetPlayer() {
        return playerToFollow;
    }

    void Update() {
        if (playerToFollow != null) {
            transform.position = playerToFollow.GetPosition() + offset;
        }
    }
}
