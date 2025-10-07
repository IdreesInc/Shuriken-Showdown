
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class HUD : UdonSharpBehaviour
{

    // Text public fields
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI powerUpsText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI playerCountText;

    private void Log(string message)
    {
        Shared.Log("HUD", message);
    }

    private void LogError(string message)
    {
        Shared.LogError("HUD", message);
    }

    /** Udon Overrides **/

    void Start()
    {
        if (statusText == null)
        {
            LogError("statusText is not set");
        }
        if (powerUpsText == null)
        {
            LogError("powerUpsText is not set");
        }
        if (scoreText == null)
        {
            LogError("scoreText is not set");
        }
        if (playerCountText == null)
        {
            LogError("playerCountText is not set");
        }
        ResetHud();
    }

    /** Custom Methods **/

    public void ResetHud()
    {
        SetPowerUps(-1, -1, -1);
        SetScore(0);
        SetLives(GameLogic.STARTING_LIVES);
    }

    public void ResetForNewRound()
    {
        SetLives(GameLogic.STARTING_LIVES);
    }

    public void SetPowerUps(int powerUpOne, int powerUpTwo, int powerUpThree)
    {
        string powerUps = "";
        if (powerUpOne != -1)
        {
            powerUps += GetFormattedPowerUp(powerUpOne);
        }
        if (powerUpTwo != -1)
        {
            powerUps += "  " + GetFormattedPowerUp(powerUpTwo);
        }
        if (powerUpThree != -1)
        {
            powerUps += "  " + GetFormattedPowerUp(powerUpThree);
        }
        powerUpsText.text = powerUps;
        Log("Updating power ups: " + powerUps);
    }

    public void SetScore(int score)
    {
        scoreText.text = "SCORE: " + score + "/" + GameLogic.Get().GetMaxScore();
    }

    public void SetPlayerCount(int playerCount, int maxPlayers)
    {
        playerCountText.text = "ALIVE: " + playerCount + "/" + maxPlayers;
    }

    public void SetLives(int lives)
    {
        string status = "ERROR";
        if (lives == 0)
        {
            status = "GHOST";
        }
        else if (lives == 1)
        {
            status = "<color=red>SHIELD DOWN</color>";
        }
        else if (lives > 1)
        {
            status = "SHIELD UP";
        }
        statusText.text = status;
        Log("Updating status: " + status);
    }

    private string GetFormattedPowerUp(int powerUp)
    {
        if (powerUp == -1)
        {
            return "";
        }
        string color = Shared.DarkenedColorStrings()[powerUp];
        return "<color=" + color + ">" + PowerUp.GetPowerUpName(powerUp).ToUpper() + "</color>";
    }
}
