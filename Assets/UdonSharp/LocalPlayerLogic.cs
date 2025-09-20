
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Local player logic that is not synced with anyone else
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LocalPlayerLogic : UdonSharpBehaviour
{

    public GameObject vrcWorld;
    // public GameObject shurikensParent;
    public GameObject playerCollidersParent;

    /// <summary>
    /// Player names indexed by player number (not player ID)
    /// </summary>
    private readonly string[] playerNames = new string[Shared.MaxPlayers() + 1];
    /// <summary>
    /// Player scores indexed by player number (not player ID)
    /// </summary>
    // private readonly int[] playerScores = new int[Shared.MaxPlayers() + 1];

    private void Log(string message)
    {
        Debug.Log("[LocalPlayerLogic]: " + message);
    }

    private void LogError(string message)
    {
        Debug.Log("[LocalPlayerLogic]: " + message);
    }

    /// <summary>
    /// Get the LocalPlayerLogic in the scene (there should only be one)
    /// </summary>
    public static LocalPlayerLogic Get()
    {
        return GameObject.Find("Local Player Logic").GetComponent<LocalPlayerLogic>();
    }

    /** Udon Overrides **/

    void Start()
    {
    }

    void Update()
    {
        /** Logic for all players **/
        // UpdatePlayerScores();
    }

    /** Custom Methods **/

    /// <summary>
    /// Triggered by the local player's shuriken when a power up is collected
    /// </summary>
    public void ShowEquippedUI(int powerUpType, int powerUpOne, int powerUpTwo, int powerUpThree)
    {
        string currentlyEquipped = "Equipped: ";
        if (powerUpOne != -1)
        {
            currentlyEquipped += PowerUp.GetPowerUpName(powerUpOne) + ", ";
        }
        if (powerUpTwo != -1)
        {
            currentlyEquipped += PowerUp.GetPowerUpName(powerUpTwo) + ", ";
        }
        if (powerUpThree != -1)
        {
            currentlyEquipped += PowerUp.GetPowerUpName(powerUpThree) + ", ";
        }
        if (currentlyEquipped.EndsWith(", "))
        {
            currentlyEquipped = currentlyEquipped.Substring(0, currentlyEquipped.Length - 2);
        }
        UIManager.Get().ShowMessageUI(
            null,
            PowerUp.GetPowerUpName(powerUpType),
            PowerUp.GetPowerUpSubtitle(powerUpType),
            currentlyEquipped,
            false,
            Shared.Colors()[powerUpType % Shared.Colors().Length],
            1300
        );
    }

    public void ShowHitUI(int playerSlot, string senderName, string verb)
    {
        int numRemaining = GameLogic.Get().GetAlivePlayerCount() - 1;
        if (numRemaining <= 0)
        {
            // Round is over, ignore this UI to wait for round update from instance owner
            return;
        }
        string remaining = "Players Remaining";
        if (numRemaining < 1)
        {
            remaining = "No " + remaining;
        }
        else
        {
            remaining = numRemaining + " " + remaining;
        }
        UIManager.Get().ShowMessageUI((verb + " by").ToUpper(),
            senderName,
            remaining,
            null,
            true,
            Shared.Colors()[playerSlot % Shared.Colors().Length],
            1500);
    }

    public void ShowKillUI(int playerSlot, string playerName)
    {
        int numRemaining = GameLogic.Get().GetAlivePlayerCount  () - 1;
        if (numRemaining <= 0)
        {
            // Round is over, ignore this UI to wait for round update from instance owner
            return;
        }
        string remaining = "Players Remaining";
        if (numRemaining < 1)
        {
            remaining = "No " + remaining;
        }
        else
        {
            remaining = numRemaining + " " + remaining;
        }
        UIManager.Get().ShowMessageUI("you sliced".ToUpper(),
            playerName,
            remaining,
            null,
            true,
            Shared.Colors()[playerSlot % Shared.Colors().Length],
            1300);
    }

    public void ShowScoreUI()
    {
        UIManager.Get().ShowScoreUI(GameLogic.Get().GetPlayerScores(), playerNames, 3000);
    }

    public void ShowGameOverUI(int winnerSlot, string winnerName)
    {
        bool won = Networking.LocalPlayer.playerId == winnerSlot;
        string[] losingMessages = {
            "It wasn't even close...",
            "Better luck next time",
            "I bet they cheated",
            "That was a fluke",
        };
        string[] winningMessages = {
            "It wasn't even close...",
            "What did they expect?",
            "Easy peasy",
        };
        string message = won ? winningMessages[Random.Range(0, winningMessages.Length)] : losingMessages[Random.Range(0, losingMessages.Length)];
        UIManager.Get().ShowMessageUI("WINNER",
            winnerName,
            message,
            null,
            true,
            Shared.Colors()[(winnerSlot - 1) % Shared.Colors().Length],
            5000);
    }

    // private void UpdatePlayerScores()
    // {
        // foreach (Transform child in shurikensParent.transform) {
        //     if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
        //         Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
        //         if (shuriken.GetPlayerNumber() != -1) {
        //             playerScores[shuriken.GetPlayerNumber()] = shuriken.GetScore();
        //             if (Networking.GetOwner(child.gameObject) != null) {
        //                 // Check needed for Unity emulator, otherwise unnecessary
        //                 playerNames[shuriken.GetPlayerNumber()] = Networking.GetOwner(child.gameObject).displayName;                        
        //             }
        //         }
        //     }
        // }
    // }

    public void SetWorldSpawn(Vector3 position)
    {
        vrcWorld.transform.position = position;
    }
}
