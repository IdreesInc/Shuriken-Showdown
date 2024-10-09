
using System.Linq;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Miner28.UdonUtils.Network;

enum UIType {
    NONE,
    SCORE_UI,
    MESSAGE_UI
}

public class GameLogic : UdonSharpBehaviour {

    public VRC.SDK3.Components.VRCObjectPool shurikenPool;
    public VRC.SDK3.Components.VRCObjectPool playerColliderPool;
    public VRC.SDK3.Components.VRCObjectPool powerUpPool;
    // Used for iterating over the shurikens
    public GameObject shurikensParent;
    public GameObject playerCollidersParent;
    public GameObject messageUI;
    public ScoreBoard scoreBoard;

    /// <summary>
    /// The current number of players
    /// </summary>
    [UdonSynced] private int numberOfPlayers = 0;

    /// <summary>
    /// Player names indexed by player number (not player ID)
    /// </summary>
    private readonly string[] playerNames = new string[Shared.MaxPlayers() + 1]; 
    /// <summary>
    /// Player scores indexed by player number (not player ID)
    /// </summary>
    private readonly int[] playerScores = new int[Shared.MaxPlayers() + 1];
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
        Debug.Log("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    private void LogError(string message) {
        LogError("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    public static GameLogic GetLocalGameLogic() {
        return GameObject.Find("Logic").GetComponent<GameLogic>();
    }

    public PlayerCollider GetLocalPlayerCollider(int playerId) {
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null && child.gameObject.GetComponent<Shuriken>().GetPlayerId() == playerId && Networking.IsOwner(child.gameObject)) {
                return child.gameObject.GetComponent<PlayerCollider>();
            }
        }
        LogError("Could not find player collider for local player " + playerId);
        return null;
    }

    public int GetAlivePlayerCount() {
        int count = 0;
        foreach (Transform child in playerCollidersParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null && child.gameObject.GetComponent<PlayerCollider>().IsAlive()) {
                count++;
            }
        }
        return count;
    }

    void Start() {
        Log("GameLogic initializing...");
        if (!Networking.IsOwner(gameObject)) {
            Log("Not the owner, skipping rest of initialization");
            return;
        }

        GameObject powerUp = powerUpPool.TryToSpawn();
        if (powerUp == null) {
            LogError("Game Logic: No available power ups");
            return;
        }
        powerUp.SetActive(true);
        PowerUp powerUpComponent = powerUp.GetComponent<PowerUp>();
        powerUpComponent.SetPowerUpType(0);
        powerUp.transform.position = new Vector3(-1, 1f, -0.3f);
    }

    void Update() {
        /** Logic for all players **/
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                if (shuriken.GetPlayerNumber() != -1) {
                    playerScores[shuriken.GetPlayerNumber()] = shuriken.GetScore();
                    playerNames[shuriken.GetPlayerNumber()] = Networking.GetOwner(child.gameObject).displayName;
                }
            }
        }
        scoreBoard.UpdateScores(playerScores, playerNames);
        UpdateUI();

        /** Logic for instance owner **/
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        int numAlive = GetAlivePlayerCount();
        if (numAlive <= 1) {
            // Round over
            // Send an event to each shuriken
            foreach (Transform child in shurikensParent.transform) {
                if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                    Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                    shuriken.SendMethodNetworked("OnRoundOver", SyncTarget.All);
                }
            }
            // Send an event to each player collider
            foreach (Transform child in playerCollidersParent.transform) {
                if (child.gameObject.activeSelf && child.gameObject.GetComponent<PlayerCollider>() != null) {
                    PlayerCollider playerCollider = child.gameObject.GetComponent<PlayerCollider>();
                    playerCollider.SendMethodNetworked("OnRoundOver", SyncTarget.All);
                }
            }
        }
    }

    public override void OnDeserialization() {
        UpdateUI();
    }

    /// <summary>
    /// Triggered by the local player's shuriken when a power up is collected
    /// </summary>
    public void OnPowerUpCollected(int powerUpType, int[] powerUps) {
        string currentlyEquipped = "Equipped: ";
        bool start = true;
        for (int i = 0; i < powerUps.Length; i++) {
            if (powerUps[i] != -1) {
                if (!start) {
                    currentlyEquipped += ", ";
                }
                currentlyEquipped += PowerUp.GetPowerUpName(powerUps[i]);
                start = false;
            }
        }

        ShowMessageUI(
            null,
            PowerUp.GetPowerUpName(powerUpType),
            PowerUp.GetPowerUpSubtitle(powerUpType),
            currentlyEquipped,
            false,
            Shared.Colors()[powerUpType % Shared.Colors().Length],
            1200
        );
    }

    public void UpdateUIOnHit(int playerNumber, string senderName, string verb) {
        int numRemaining = GetAlivePlayerCount();
        if (numRemaining <= 1) {
            // Round is over, ignore this UI to wait for round update from instance owner
            return;
        }
        string remaining = "Players Remaining";
        if (numRemaining < 1) {
            remaining = "No " + remaining;
        } else {
            remaining = numRemaining + " " + remaining;
        }
        ShowMessageUI((verb + " by").ToUpper(),
            senderName,
            remaining,
            null,
            true,
            Shared.Colors()[(playerNumber - 1) % Shared.Colors().Length],
            1500);
    }

    public void UpdateUIOnRoundOver() {
        ShowScoreUI(3000);
    }

    private void ShowMessageUI(
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

    private void ShowScoreUI(float duration = 2000) {
        timeToShowUI = Time.time * 1000 + UI_FADE_TIME;
        timeToHideUI = timeToShowUI + duration + UI_FADE_TIME;
        visibleUI = UIType.SCORE_UI;
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
            messageUI.SetActive(true);
            visibleUIObject = scoreBoard.gameObject;
        } else {
            messageUI.SetActive(false);
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

    public override void OnPlayerJoined(VRCPlayerApi player) {
        Log("Player joined: " + player.displayName);

        if (!Networking.IsOwner(gameObject)) {
            Log("A player joined but we are not the owner so who cares");
            return;
        }

        if (player == null || !Utilities.IsValid(player)) {
            LogError("Somehow, the player is null in OnPlayerJoined");
            return;
        }

        // Increase player speed
        // player.SetWalkSpeed(5);
        // player.SetRunSpeed(8);
        // // Increase player jump height
        // player.SetJumpImpulse(5);

        if (playerColliderPool == null) {
            LogError("Player Collider Pool is not set");
            return;
        } else if (player == null) {
            LogError("Interacting player is not set");
            return;
        } else if (shurikenPool == null) {
            LogError("Shuriken Pool is not set");
            return;
        } else if (powerUpPool == null) {
            LogError("Power Up Pool is not set");
            return;
        }

        if (numberOfPlayers == Shared.MaxPlayers()) {
            LogError("Max players reached, not adding components for " + player.playerId);
            return;
        }

        numberOfPlayers++;

        // Assign a shuriken to the player
        GameObject shuriken = shurikenPool.TryToSpawn();
        if (shuriken == null) {
            LogError("No available shurikens");
            return;
        }
        shuriken.SetActive(true);
        Shuriken shurikenComponent = shuriken.GetComponent<Shuriken>();
        shurikenComponent.SetPlayerId(player.playerId);
        shurikenComponent.SetPlayerNumber(numberOfPlayers);
        shurikenComponent.ReturnToPlayer();


        // Assign a player collider to the player
        GameObject playerCollider = playerColliderPool.TryToSpawn();
        if (playerCollider == null) {
            LogError("No available player colliders");
            return;
        }
        playerCollider.SetActive(true);
        PlayerCollider playerColliderComponent = playerCollider.GetComponent<PlayerCollider>();
        playerColliderComponent.SetPlayerId(player.playerId);
    }
}
