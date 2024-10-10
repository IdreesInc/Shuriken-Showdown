
using UdonSharp;
using UnityEngine;
using VRC.Udon.Common.Interfaces;

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
        // Send message to GameLogic to start the game
        GameLogic.GetGameLogic().SendCustomNetworkEvent(NetworkEventTarget.Owner, "StartGame");
    }
}
