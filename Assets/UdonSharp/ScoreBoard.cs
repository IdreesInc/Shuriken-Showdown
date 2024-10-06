
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using VRC.SDKBase;
using VRC.Udon;

public class ScoreBoard : UdonSharpBehaviour {

    public GameObject[] scoreLines;

    private void Log(string message) {
        Debug.Log("[ScoreBoard]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[ScoreBoard]: " + message);
    }

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

    public void updateScores(int[] scores) {
        for (int i = 0; i < scores.Length; i++) {
            updateScore(i, scores[i]);
        }
    }

    private void updateScore(int playerId, int score) {
        if (playerId < 1 || playerId > 8) {
            return;
        }
        // Get the scoreLine for the player
        GameObject scoreLine = scoreLines[playerId - 1];
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
