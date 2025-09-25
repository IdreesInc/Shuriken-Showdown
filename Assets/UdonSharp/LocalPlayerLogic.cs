
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
    public GameObject playerCollidersParent;

    private void Log(string message)
    {
        Shared.Log("LocalPlayerLogic", message);
    }

    private void LogError(string message)
    {
        Shared.LogError("LocalPlayerLogic", message);
    }

    /// <summary>
    /// Get the LocalPlayerLogic in the scene (there should only be one)
    /// </summary>
    public static LocalPlayerLogic Get()
    {
        return GameObject.Find("Local Player Logic").GetComponent<LocalPlayerLogic>();
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
        LocalUIManager.Get().ShowMessageUI(
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
        LocalUIManager.Get().ShowMessageUI((verb + " by").ToUpper(),
            senderName,
            remaining,
            null,
            true,
            Shared.Colors()[playerSlot % Shared.Colors().Length],
            1500);
    }

    public void ShowKillUI(int playerSlot, string playerName, string verb)
    {
        // Alive player count might not yet be updated, calculate it manually
        bool[] statuses = GameLogic.Get().GetPlayerAliveStatuses();
        int numRemaining = 0;
        int currentPlayerSlot = GameLogic.Get().GetPlayerSlot(Networking.LocalPlayer.playerId);
        for (int i = 0; i < statuses.Length; i++)
        {
            if (statuses[i] && i != playerSlot && i != currentPlayerSlot)
            {
                numRemaining++;
            }
        }
        if (numRemaining <= 0)
        {
            // Round is over, ignore this UI to wait for round update from instance owner
            return;
        }
        string remaining;
        if (numRemaining < 1)
        {
            remaining = "No Players Remaining";
        }
        else if (numRemaining == 1)
        {
            remaining = "1 Player Remaining";
        }
        else
        {
            remaining = numRemaining + " Players Remaining";
        }
        LocalUIManager.Get().ShowMessageUI(("you " + verb).ToUpper(),
            playerName,
            remaining,
            null,
            true,
            Shared.Colors()[playerSlot % Shared.Colors().Length],
            1300);
    }

    public void ShowScoreUI()
    {
        LocalUIManager.Get().ShowScoreUI(3000);
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
        LocalUIManager.Get().ShowMessageUI("WINNER",
            winnerName,
            message,
            null,
            true,
            Shared.Colors()[(winnerSlot - 1) % Shared.Colors().Length],
            5000);
    }

    public void SetWorldSpawn(Vector3 position)
    {
        vrcWorld.transform.position = position;
    }
}
