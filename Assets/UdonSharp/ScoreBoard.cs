
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// Non-networked UdonSharpBehaviour for updating the score board UI
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ScoreBoard : UdonSharpBehaviour
{

    public GameObject[] scoreLines;

    private void Log(string message)
    {
        Shared.Log("ScoreBoard", message, Networking.GetOwner(gameObject));
    }

    private void LogError(string message)
    {
        Shared.LogError("ScoreBoard", message, Networking.GetOwner(gameObject));
    }

    /** Udon Overrides **/

    void Start()
    {
        if (scoreLines == null || scoreLines.Length == 0)
        {
            LogError("scoreLines is null or empty");
        }
        Log(Shared.DarkenedColors().Length + " colors available");
        // Set the color of each score line
        for (int i = 0; i < scoreLines.Length; i++)
        {
            Color color = Shared.DarkenedColors()[i];
            scoreLines[i].GetComponent<UnityEngine.UI.Image>().color = color;
        }
    }

    /** Custom Methods **/

    public void UpdateScores()
    {
        GameLogic gameLogic = GameLogic.Get();
        int[] scores = gameLogic.GetPlayerScores();
        int[] playerSlots = gameLogic.GetPlayerSlots();
        for (int i = 0; i < playerSlots.Length; i++)
        {
            int slot = playerSlots[i];
            if (slot > 0)
            {
                VRCPlayerApi player = VRCPlayerApi.GetPlayerById(slot);
                string name = "Unknown";
                if (player != null)
                {
                    name = player.displayName;
                }
                UpdateScore(i, name, scores[i]);
            }
            else
            {
                HideScoreLine(i);
            }
        }
    }

    private void UpdateScore(int playerSlot, string name, int score)
    {
        // Get the scoreLine for the player
        GameObject scoreLine = scoreLines[playerSlot];
        scoreLine.SetActive(true);
        GameObject scoreName = scoreLine.transform.Find("Score Name").gameObject;
        GameObject dotsContainer = scoreLine.transform.Find("Dots Container").gameObject;
        // Get the Text component of the scoreName
        scoreName.GetComponent<TextMeshProUGUI>().text = name;
        for (int i = 0; i < 10; i++)
        {
            // Get child of scoreLine with name "Score Dot " + i
            GameObject scoreDot = dotsContainer.transform.Find("Score Dot " + i).gameObject;
            // Get the Image component of the scoreDot
            UnityEngine.UI.Image image = scoreDot.GetComponent<UnityEngine.UI.Image>();
            // If the score is greater than i, set the alpha to 1, otherwise set it to 0.5
            image.color = new Color(image.color.r, image.color.g, image.color.b, score > i ? 1 : 0.5f);
            // Set active/inactive based on max score
            scoreDot.SetActive(i < GameLogic.Get().GetMaxScore());
        }
    }

    private void HideScoreLine(int playerSlot)
    {
        GameObject scoreLine = scoreLines[playerSlot];
        scoreLine.SetActive(false);
    }
}
