
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;

enum UIType {
    NONE,
    SCORE_UI,
    MESSAGE_UI
}

/// <summary>
/// Non-networked UdonSharpBehaviour for managing the local UI
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class UIManager : UdonSharpBehaviour {

    public GameObject messageUI;
    public ScoreBoard scoreBoard;

    /// <summary>
    /// The currently visible UI
    /// </summary>
    private UIType visibleUI = UIType.NONE;
    private const float UI_FADE_TIME = 200;
    /// <summary>
    /// The time at which the current UI should be fully visible (in milliseconds)
    /// </summary>
    private float timeToShowUI = 0;
    /// <summary>
    /// The time at which the current UI should be fully hidden (in milliseconds)
    /// </summary>
    private float timeToHideUI = 0;

    private void Log(string message) {
        Debug.Log("[UIManager]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[UIManager]: " + message);
    }

    public static UIManager GetUIManager() {
        return GameObject.Find("UI Manager").GetComponent<UIManager>();
    }

    public void ShowMessageUI(
        string topText = "", 
        string highlightText = "", 
        string middleText = "", 
        string bottomText = "", 
        bool backgroundEnabled = true, 
        Color highlightColor = new Color(),
        float duration = 2000) {

        SetMessageUI(topText, highlightText, middleText, bottomText, backgroundEnabled, highlightColor);
        timeToShowUI = Time.time * 1000 + UI_FADE_TIME;
        timeToHideUI = timeToShowUI + duration + UI_FADE_TIME;
        visibleUI = UIType.MESSAGE_UI;
        UpdateUI();
    }

    public void ShowScoreUI(int[] playerScores, string[] playerNames, float duration = 2000) {
        timeToShowUI = Time.time * 1000 + UI_FADE_TIME;
        timeToHideUI = timeToShowUI + duration + UI_FADE_TIME;
        visibleUI = UIType.SCORE_UI;
        scoreBoard.UpdateScores(playerScores, playerNames);
        UpdateUI();
    }

    private void SetMessageUI(
        string topText = "", 
        string highlightText = "", 
        string middleText = "", 
        string bottomText = "", 
        bool backgroundEnabled = true, 
        Color highlightColor = new Color()) {

        GameObject background = messageUI.transform.Find("Background").gameObject;
        GameObject topTextUI = messageUI.transform.Find("Top Text").gameObject;
        GameObject highlight = messageUI.transform.Find("Highlight").gameObject;
        GameObject highlightTextUI = highlight.transform.Find("Highlight Text").gameObject;
        GameObject middleTextUI = messageUI.transform.Find("Middle Text").gameObject;
        GameObject bottomTextUI = messageUI.transform.Find("Bottom Text").gameObject;

        if (background == null) {
            LogError("Background is null");
            return;
        } else if (topTextUI == null) {
            LogError("Top Text is null");
            return;
        } else if (highlight == null) {
            LogError("Highlight is null");
            return;
        } else if (highlightTextUI == null) {
            LogError("Highlight Text is null");
            return;
        } else if (middleTextUI == null) {
            LogError("Middle Text is null");
            return;
        } else if (bottomTextUI == null) {
            LogError("Bottom Text is null");
            return;
        }

        background.SetActive(backgroundEnabled);
        topTextUI.GetComponent<TextMeshProUGUI>().text = topText;
        highlightTextUI.GetComponent<TextMeshProUGUI>().text = highlightText;
        middleTextUI.GetComponent<TextMeshProUGUI>().text = middleText;
        bottomTextUI.GetComponent<TextMeshProUGUI>().text = bottomText;
        highlight.GetComponent<UnityEngine.UI.Image>().color = highlightColor;
    }

    private void UpdateUI() {
        GameObject visibleUIObject = null;
        float alpha = 1;
        float timeInMs = Time.time * 1000;

        // Fade in
        if (timeToShowUI != 0 && timeInMs >= timeToShowUI - UI_FADE_TIME) {
            if (timeInMs >= timeToShowUI) {
                timeToShowUI = 0;
                alpha = 1;
            } else {
                alpha = 1 - (timeToShowUI - timeInMs) / UI_FADE_TIME;
            }
        }

        // Fade out
        if (timeToHideUI != 0 && timeInMs >= timeToHideUI - UI_FADE_TIME) {
            if (timeInMs >= timeToHideUI) {
                visibleUI = UIType.NONE;
                timeToHideUI = 0;
                alpha = 0;
            } else {
                alpha = (timeToHideUI - timeInMs) / UI_FADE_TIME;
            }
        }
        
        if (visibleUI == UIType.MESSAGE_UI) {
            messageUI.SetActive(true);
            visibleUIObject = messageUI;
        } else if (visibleUI == UIType.SCORE_UI) {
            scoreBoard.gameObject.SetActive(true);
            visibleUIObject = scoreBoard.gameObject;
        } else {
            messageUI.SetActive(false);
            scoreBoard.gameObject.SetActive(false);
        }

        if (visibleUIObject != null) {
            // Put UI in front of player's camera
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            // Get the player's head tracking data (camera position and rotation)
            VRCPlayerApi.TrackingData headData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            // Calculate position in front of the player's camera
            Vector3 newPosition = headData.position + headData.rotation * Vector3.forward * 1.25f;

            // Set the target object's position and rotation
            visibleUIObject.transform.position = newPosition;
            visibleUIObject.transform.rotation = headData.rotation;

            visibleUIObject.GetComponent<CanvasGroup>().alpha = alpha;
        }
    }

    void Update() {
        UpdateUI();
    }
}
