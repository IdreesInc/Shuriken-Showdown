
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using VRC.SDK3.Persistence;

public class StatsUI : UdonSharpBehaviour
{

    public TextMeshProUGUI playersHit;
    public TextMeshProUGUI playersKilled;
    public TextMeshProUGUI powerUps;
    public TextMeshProUGUI targetsHit;
    public TextMeshProUGUI gamesPlayed;
    public TextMeshProUGUI gamesWon;
    public TextMeshProUGUI totalPoints;

    private const int TEXT_LENGTH = 19;

    void Start()
    {
        UpdateStats();
    }

    // On player updated
    public override void OnPlayerDataUpdated(VRCPlayerApi player, PlayerData.Info[] infos)
    {
        if (!player.isLocal)
        {
            return;
        }
        UpdateStats();
    }

    private void UpdateStats()
    {
        string[] colors = Shared.ColorStrings();
        SetTextWithPadding(totalPoints, "TOTAL POINTS", CalculateTotalPoints(), colors[0]);
        SetTextWithPadding(playersHit, "PLAYERS HIT", Shared.GetStat(PlayerStats.PLAYERS_HIT), colors[1]);
        SetTextWithPadding(playersKilled, "PLAYERS KILLED", Shared.GetStat(PlayerStats.PLAYERS_KILLED), colors[2]);
        SetTextWithPadding(powerUps, "POWER-UPS", Shared.GetStat(PlayerStats.POWER_UPS_COLLECTED), colors[3]);
        SetTextWithPadding(targetsHit, "TARGETS HIT", Shared.GetStat(PlayerStats.TARGETS_HIT), colors[4]);
        SetTextWithPadding(gamesPlayed, "GAMES PLAYED", Shared.GetStat(PlayerStats.GAMES_PLAYED), colors[5]);
        SetTextWithPadding(gamesWon, "GAMES WON", Shared.GetStat(PlayerStats.GAMES_WON), colors[6]);
    }

    private void SetTextWithPadding(TMP_Text textComponent, string label, int value, string color = "#000000")
    {
        string start = label + ": ";
        string end = value.ToString();
        int remainingLength = TEXT_LENGTH - start.Length;
        textComponent.text = $"{start}<color={color}>{end.PadLeft(remainingLength, ' ')}</color>";
    }

    private int CalculateTotalPoints()
    {
        int total = 0;
        total += Shared.GetStat(PlayerStats.PLAYERS_HIT) * 1;
        total += Shared.GetStat(PlayerStats.PLAYERS_KILLED) * 2;
        total += Shared.GetStat(PlayerStats.POWER_UPS_COLLECTED) * 1;
        total += Shared.GetStat(PlayerStats.TARGETS_HIT) * 1;
        total += Shared.GetStat(PlayerStats.GAMES_PLAYED) * 5;
        total += Shared.GetStat(PlayerStats.GAMES_WON) * 25;
        return total * 5;
    }
}
