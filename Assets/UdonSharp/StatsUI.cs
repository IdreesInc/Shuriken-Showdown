
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
        SetTextWithPadding(playersHit, "PLAYERS HIT", Shared.GetStat(PlayerStats.PLAYERS_HIT));
        SetTextWithPadding(playersKilled, "PLAYERS KILLED", Shared.GetStat(PlayerStats.PLAYERS_KILLED));
        SetTextWithPadding(powerUps, "POWER-UPS", Shared.GetStat(PlayerStats.POWER_UPS_COLLECTED));
        SetTextWithPadding(targetsHit, "TARGETS HIT", Shared.GetStat(PlayerStats.TARGETS_HIT));
        SetTextWithPadding(gamesPlayed, "GAMES PLAYED", Shared.GetStat(PlayerStats.GAMES_PLAYED));
        SetTextWithPadding(gamesWon, "GAMES WON", Shared.GetStat(PlayerStats.GAMES_WON));
    }

    private void SetTextWithPadding(TMP_Text textComponent, string label, int value)
    {
        int padding = TEXT_LENGTH - label.Length - value.ToString().Length - 1; // -1 for ":"
        textComponent.text = $"{label}: {value.ToString().PadLeft(padding, ' ')}";
    }
}
