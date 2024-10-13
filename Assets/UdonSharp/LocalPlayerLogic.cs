
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Local player logic that is not synced with anyone else
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LocalPlayerLogic : UdonSharpBehaviour {

    public GameObject shurikensParent;
    public GameObject playerCollidersParent; 

    /// <summary>
    /// Player names indexed by player number (not player ID)
    /// </summary>
    private readonly string[] playerNames = new string[Shared.MaxPlayers() + 1]; 
    /// <summary>
    /// Player scores indexed by player number (not player ID)
    /// </summary>
    private readonly int[] playerScores = new int[Shared.MaxPlayers() + 1];

    private void Log(string message) {
        Debug.Log("[LocalPlayerLogic]: " + message);
    }

    private void LogError(string message) {
        Debug.Log("[LocalPlayerLogic]: " + message);
    }

    /// <summary>
    /// Get the LocalPlayerLogic in the scene (there should only be one)
    /// </summary>
    public static LocalPlayerLogic Get() {
        return GameObject.Find("Local Player Logic").GetComponent<LocalPlayerLogic>();
    }

    /** Udon Overrides **/

    void Start() {
    }

    void Update() {
        /** Logic for all players **/
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                if (shuriken.GetPlayerNumber() != -1) {
                    playerScores[shuriken.GetPlayerNumber()] = shuriken.GetScore();
                    if (Networking.GetOwner(child.gameObject) != null) {
                        // Check needed for Unity emulator, otherwise unnecessary
                        playerNames[shuriken.GetPlayerNumber()] = Networking.GetOwner(child.gameObject).displayName;                        
                    }
                }
            }
        }
    }

    /** Custom Methods **/

    /// <summary>
    /// Triggered by the local player's shuriken when a power up is collected
    /// </summary>
    public void ShowEquippedUI(int powerUpType, int powerUpOne, int powerUpTwo, int powerUpThree) {
        string currentlyEquipped = "Equipped: ";
        if (powerUpOne != -1) {
            currentlyEquipped += PowerUp.GetPowerUpName(powerUpOne) + ", ";
        }
        if (powerUpTwo != -1) {
            currentlyEquipped += PowerUp.GetPowerUpName(powerUpTwo) + ", ";
        }
        if (powerUpThree != -1) {
            currentlyEquipped += PowerUp.GetPowerUpName(powerUpThree) + ", ";
        }
        if (currentlyEquipped.EndsWith(", ")) {
            currentlyEquipped = currentlyEquipped.Substring(0, currentlyEquipped.Length - 2);
        }
        UIManager.Get().ShowMessageUI(
            null,
            PowerUp.GetPowerUpName(powerUpType),
            PowerUp.GetPowerUpSubtitle(powerUpType),
            currentlyEquipped,
            false,
            Shared.Colors()[powerUpType % Shared.Colors().Length],
            1200
        );
    }

    public void ShowHitUI(int playerNumber, string senderName, string verb) {
        int numRemaining = GetAlivePlayerCount() - 1;
        if (numRemaining <= 0) {
            // Round is over, ignore this UI to wait for round update from instance owner
            return;
        }
        string remaining = "Players Remaining";
        if (numRemaining < 1) {
            remaining = "No " + remaining;
        } else {
            remaining = numRemaining + " " + remaining;
        }
        UIManager.Get().ShowMessageUI((verb + " by").ToUpper(),
            senderName,
            remaining,
            null,
            true,
            Shared.Colors()[(playerNumber - 1) % Shared.Colors().Length],
            1500);
    }

    public void ShowScoreUI() {
        UIManager.Get().ShowScoreUI(playerScores, playerNames, 3000);
    }

    public void ShowGameOverUI(int winnerNumber, string winnerName) {
        UIManager.Get().ShowMessageUI("WINNER",
            winnerName,
            "It wasn't even close",
            null,
            true,
            Shared.Colors()[(winnerNumber - 1) % Shared.Colors().Length],
            3000);
    }

    public int GetAlivePlayerCount() {
        int count = 0;
        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null && child.gameObject.GetComponent<PlayerCollider>().IsAlive()) {
                count++;
            }
        }
        return count;
    }
}
