
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

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
    public GameObject messageUI;

    private int[] playerScores = new int[16];

    private UIType currentUI = UIType.MESSAGE_UI;

    
    private void Log(string message) {
        Debug.Log("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
    }

    private void LogError(string message) {
        LogError("[GameLogic - " + Networking.LocalPlayer.playerId + "]: " + message);
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
        SetMessage("Toppy", "Highlight", "Middle", "Bottom");
    }

    void Update() {
        foreach (Transform child in shurikensParent.transform) {
            if (child.gameObject.activeSelf && child.gameObject.GetComponent<Shuriken>() != null) {
                Shuriken shuriken = child.gameObject.GetComponent<Shuriken>();
                if (shuriken.GetPlayerId() != -1) {
                    playerScores[shuriken.GetPlayerId()] = shuriken.GetScore();
                }
            }
        }
        UpdateUI();
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
    }

    public override void OnDeserialization() {
        UpdateUI();
    }

    private void SetMessage(string topText = "", string highlightText = "", string middleText = "", string bottomText = "") {
        GameObject background = messageUI.transform.Find("Background").gameObject;
        GameObject topTextUI = messageUI.transform.Find("Top Text").gameObject;
        GameObject highlight = messageUI.transform.Find("Highlight").gameObject;
        GameObject highlightTextUI = highlight.transform.Find("Highlight Text").gameObject;
        GameObject middleTextUI = messageUI.transform.Find("Middle Text").gameObject;
        GameObject bottomTextUI = messageUI.transform.Find("Bottom Text").gameObject;

        if (background == null) {
            LogError("Game Logic: Background is null");
            return;
        } else if (topTextUI == null) {
            LogError("Game Logic: Top Text is null");
            return;
        } else if (highlight == null) {
            LogError("Game Logic: Highlight is null");
            return;
        } else if (highlightTextUI == null) {
            LogError("Game Logic: Highlight Text is null");
            return;
        } else if (middleTextUI == null) {
            LogError("Game Logic: Middle Text is null");
            return;
        } else if (bottomTextUI == null) {
            LogError("Game Logic: Bottom Text is null");
            return;
        }
        
        topTextUI.GetComponent<TextMeshProUGUI>().text = topText;
        highlightTextUI.GetComponent<TextMeshProUGUI>().text = highlightText;
        middleTextUI.GetComponent<TextMeshProUGUI>().text = middleText;
        bottomTextUI.GetComponent<TextMeshProUGUI>().text = bottomText;
    }

    private void UpdateUI() {
        GameObject ui = null;
        if (currentUI == UIType.MESSAGE_UI) {
            messageUI.SetActive(true);
            ui = messageUI;
        } else {
            messageUI.SetActive(false);
        }

        if (ui != null) {
            // Put UI in front of player's camera
            VRCPlayerApi localPlayer = Networking.LocalPlayer;
            // Get the player's head tracking data (camera position and rotation)
            VRCPlayerApi.TrackingData headData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

            // Calculate position in front of the player's camera
            Vector3 newPosition = headData.position + headData.rotation * Vector3.forward * 1.25f;

            // Set the target object's position and rotation
            ui.transform.position = newPosition;
            ui.transform.rotation = headData.rotation;
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player) {
        Log("Player joined: " + player.displayName);

        if (!Networking.IsOwner(gameObject)) {
            Log("A player joined but we are not the owner so who cares");
            return;
        }

        if (player == null || !Utilities.IsValid(player)) {
            LogError("Game Logic: Somehow, the player is null in OnPlayerJoined");
            return;
        }

        // Increase player speed
        // player.SetWalkSpeed(5);
        // player.SetRunSpeed(8);
        // // Increase player jump height
        // player.SetJumpImpulse(5);

        if (playerColliderPool == null) {
            LogError("Game Logic: Player Collider Pool is not set");
            return;
        } else if (player == null) {
            LogError("Game Logic: Interacting player is not set");
            return;
        } else if (shurikenPool == null) {
            LogError("Game Logic: Shuriken Pool is not set");
            return;
        } else if (powerUpPool == null) {
            LogError("Game Logic: Power Up Pool is not set");
            return;
        }

        // Assign a shuriken to the player
        GameObject shuriken = shurikenPool.TryToSpawn();
        if (shuriken == null) {
            LogError("Game Logic: No available shurikens");
            return;
        }
        shuriken.SetActive(true);
        Shuriken shurikenComponent = shuriken.GetComponent<Shuriken>();
        shurikenComponent.SetPlayerId(player.playerId);
        shurikenComponent.ReturnToPlayer();


        // Assign a player collider to the player
        GameObject playerCollider = playerColliderPool.TryToSpawn();
        if (playerCollider == null) {
            LogError("Game Logic: No available player colliders");
            return;
        }
        playerCollider.SetActive(true);
        PlayerCollider playerColliderComponent = playerCollider.GetComponent<PlayerCollider>();
        playerColliderComponent.SetPlayerId(player.playerId);
    }
}
