
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VRC.SDKBase;
using VRC.Udon;

/// <summary>
/// Non-networked UdonSharpBehaviour for updating the score board UI
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ScoreBoard : UdonSharpBehaviour {

    public GameObject[] scoreLines;

    private void Log(string message) {
        Debug.Log("[ScoreBoard]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[ScoreBoard]: " + message);
    }

    /** Udon Overrides **/

    void Start() {
        if (scoreLines == null || scoreLines.Length == 0) {
            LogError("scoreLines is null or empty");
        }
        Log(Shared.Colors().Length + " colors available");
        // Set the color of each score line
        for (int i = 0; i < scoreLines.Length; i++) {
            scoreLines[i].GetComponent<UnityEngine.UI.Image>().color = Shared.Colors()[i];
        }
    }

    /** Custom Methods **/

    public void UpdateScores(int[] scores, string[] names) {
        // Start iterating at one since there is no player 0
        for (int i = 1; i < scores.Length; i++) {
            // Log("Updating score for player " + i + " to " + scores[i]);
            UpdateScore(i, names[i], scores[i]);
        }
    }

    private void UpdateScore(int playerNumber, string name, int score) {
        // Get the scoreLine for the player
        GameObject scoreLine = scoreLines[playerNumber - 1];
        GameObject scoreName = scoreLine.transform.Find("Score Name").gameObject;
        // Get the Text component of the scoreName
        scoreName.GetComponent<TextMeshProUGUI>().text = name;
        for (int i = 0; i < 10; i++) {
            // Get child of scoreLine with name "Score Dot " + i
            GameObject scoreDot = scoreLine.transform.Find("Score Dot " + i).gameObject;
            // Get the Image component of the scoreDot
            UnityEngine.UI.Image image = scoreDot.GetComponent<UnityEngine.UI.Image>();
            // If the score is greater than i, set the alpha to 1, otherwise set it to 0.5
            image.color = new Color(image.color.r, image.color.g, image.color.b, score > i ? 1 : 0.3f);
        }
    }
}
