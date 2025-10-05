
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

    private string powerUps = "";
    private string score = "";

    public static HUD Get()
    {
        return GameObject.Find("HUD").GetComponent<HUD>();
    }

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
    }

    void Update()
    {
        statusText.text = "SHIELD UP";
        powerUpsText.text = powerUps;
        scoreText.text = "SCORE: " + score;
    }

    /** Custom Methods **/

    public void SetPowerUps(int powerUpOne, int powerUpTwo, int powerUpThree)
    {
        this.powerUps = "";
        if (powerUpOne != -1)
        {
            this.powerUps += GetFormattedPowerUp(powerUpOne);
        }
        if (powerUpTwo != -1)
        {
            this.powerUps += " " + GetFormattedPowerUp(powerUpTwo);
        }
        if (powerUpThree != -1)
        {
            this.powerUps += " " + GetFormattedPowerUp(powerUpThree);
        }
        Log("Updating power ups: " + this.powerUps);
    }

    public void SetScore(int score)
    {
        this.score = score + "/" + GameLogic.Get().GetMaxScore();
        Log("Updating score: " + score);
    }

    private string GetFormattedPowerUp(int powerUp)
    {
        if (powerUp == -1)
        {
            return "";
        }
        string color = Shared.ColorStrings()[powerUp];
        return "<color=" + color + ">" + PowerUp.GetPowerUpName(powerUp).ToUpper() + "</color>";
    }
}
