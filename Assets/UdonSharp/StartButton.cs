
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StartButton : UdonSharpBehaviour {

    private void Log(string message) {
        Debug.Log("[StartButton]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[StartButton]: " + message);
    }
    void Start() {
        
    }

    // On collision
    void OnTriggerEnter(Collider other) {
        Log("Ya hit the start button");
        if (Networking.IsOwner(other.gameObject)) {
            // Send message to GameLogic to start the game
            GameLogic.GetGameLogic().SendMethodNetworked(nameof(GameLogic.StartGame), SyncTarget.All);
        }
    }
}
