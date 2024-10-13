
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class MainMenu : UdonSharpBehaviour {

    public GameObject[] playerIcons;
    public GameObject gameMasterText;

    /** Udon Overrides */

    private void Log(string message) {
        Debug.Log("[MainMenu]: " + message);
    }

    private void LogError(string message) {
        Debug.Log("[MainMenu]: " + message);
    }


    void Start() {
        // OnDeserialization();
    }

    void Update() {
        // TODO: Only update when necessary
        UpdateGameMaster();
        UpdatePlayerIcons();
    }

    public void OnStartGamePressed() {
        Log("Start Game Pressed");
        GameLogic.Get().SendMethodNetworked(nameof(GameLogic.StartGame), SyncTarget.All);
    }

    /** Custom Methods */

    private void UpdateGameMaster() {
        VRCPlayerApi gameMaster = Networking.GetOwner(gameObject);
        gameMasterText.GetComponent<TextMeshProUGUI>().text = "Game Master: " + gameMaster.displayName;
    }

    private void UpdatePlayerIcons() {
        GameLogic gameLogic = GameLogic.Get();
        bool[] activePlayers = gameLogic.GetActivePlayers();
        for (int i = 0; i < playerIcons.Length; i++) {
            if (activePlayers[i]) {
                playerIcons[i].GetComponent<Image>().color = Shared.Colors()[i];
                playerIcons[i].transform.Find("Player Icon Text").GetComponent<TextMeshProUGUI>().text = "P" + (i + 1);
            } else {
                playerIcons[i].GetComponent<Image>().color = new Color(0, 0, 0, 0.1f);
                playerIcons[i].transform.Find("Player Icon Text").GetComponent<TextMeshProUGUI>().text = "";
            }
        }
    }
}
