
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;
using VRC.Udon.Common.Interfaces;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class MainMenu : UdonSharpBehaviour
{
    public GameLogic gameLogic;
    public GameObject[] playerIcons;
    public GameObject[] gameMasterTexts;
    public GameObject settingsUI;
    public TextMeshProUGUI timerText;
    public Button startButton;
    public Button joinButton;

    /// <summary>
    /// Delay in seconds before non-owners see the game can be started
    /// </summary>
    private const int NON_OWNER_DELAY = 20;
    /// <summary>
    /// Delay in seconds before owners can restart the game
    /// </summary>
    private const int OWNER_DELAY = 5;
    private readonly Color JOIN_COLOR = Shared.HexToColor("#2FD93C");
    private readonly Color SPECTATE_COLOR = Shared.HexToColor("#FB9E38");

    [UdonSynced] private float nonOwnerStartTime = 0f;
    [UdonSynced] private float ownerStartTime = 0f;

    /** Udon Overrides */

    private void Log(string message)
    {
        Shared.Log("MainMenu", message, Networking.GetOwner(gameObject));
    }

    private void LogError(string message)
    {
        Shared.LogError("MainMenu", message, Networking.GetOwner(gameObject));
    }

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            ResetTimer();
        }
        settingsUI.SetActive(false);
    }

    void Update()
    {
        // TODO: Only update when necessary
        UpdateGameMaster();
        UpdatePlayerIcons();
        UpdateTimer();
        UpdateStartButton();
        UpdateJoinButton();
    }

    public void OnStartGamePressed()
    {
        Log("Start Game Pressed");
        gameLogic.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GameLogic.StartGame));
    }

    public void OnJoinPressed()
    {
        Log("Join Pressed");
        if (gameLogic.HasPlayerJoined(Networking.LocalPlayer.playerId))
        {
            gameLogic.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GameLogic.RequestLeave));
        }
        else
        {
            gameLogic.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GameLogic.RequestJoin));
        }
    }

    public void OnShowSettingsPressed()
    {
        Log("Show Settings Pressed");
        settingsUI.SetActive(true);
    }

    public void OnHideSettingsPressed()
    {
        Log("Hide Settings Pressed");
        settingsUI.SetActive(false);
    }

    /** Custom Methods */

    public void ResetTimer()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        nonOwnerStartTime = Time.time + NON_OWNER_DELAY;
        ownerStartTime = Time.time + OWNER_DELAY;
        UpdateTimer();
    }

    private void UpdateGameMaster()
    {
        VRCPlayerApi gameMaster = Networking.GetOwner(gameObject);
        if (gameMaster == null)
        {
            return;
        }
        foreach (GameObject gameMasterText in gameMasterTexts)
        {
            gameMasterText.GetComponent<TextMeshProUGUI>().text = "Game Master: " + gameMaster.displayName;
        }
    }

    private void UpdatePlayerIcons()
    {
        GameLogic gameLogic = GameLogic.Get();
        for (int i = 0; i < playerIcons.Length; i++)
        {
            if (gameLogic.IsPlayerSlotActive(i))
            {
                playerIcons[i].GetComponent<Image>().color = Shared.Colors()[i];
                playerIcons[i].transform.Find("Player Icon Text").GetComponent<TextMeshProUGUI>().text = "P" + (i + 1);
            }
            else
            {
                playerIcons[i].GetComponent<Image>().color = new Color(0, 0, 0, 0.1f);
                playerIcons[i].transform.Find("Player Icon Text").GetComponent<TextMeshProUGUI>().text = "";
            }
        }
    }

    private void UpdateTimer()
    {
        if (gameLogic.GetCurrentGameState() != GameState.Lobby)
        {
            timerText.text = "Game is currently in progress";
            return;
        } else if (gameLogic.GetJoinedPlayerSlotCount() == 0)
        {
            timerText.text = "Join the game to begin";
            return;
        }
        float startTime = GetStartTime();
        if (startTime == 0f)
        {
            timerText.text = "";
            return;
        }
        float timeRemaining = startTime - Time.time;
        if (timeRemaining <= 0f)
        {
            timerText.text = "";
            return;
        }
        int seconds = Mathf.CeilToInt(timeRemaining);
        timerText.text = "Next game available in " + seconds + " second" + (seconds == 1 ? "" : "s");
    }

    private void UpdateStartButton()
    {
        if (gameLogic.GetCurrentGameState() != GameState.Lobby)
        {
            startButton.interactable = false;
            return;
        }
        bool canStart = false;
        float startTime = GetStartTime();
        if (startTime != 0f && Time.time >= startTime)
        {
            canStart = true;
        }
        if (gameLogic.GetJoinedPlayerSlotCount() == 0)
        {
            // Can't start the game if nobody has joined
            canStart = false;
        }
        startButton.interactable = canStart;
    }

    private void UpdateJoinButton()
    {
        bool joined = gameLogic.HasPlayerJoined(Networking.LocalPlayer.playerId);
        if (joined)
        {
            joinButton.transform.Find("Join Button Text").GetComponent<TextMeshProUGUI>().text = "SPECTATE";
            joinButton.GetComponent<Image>().color = SPECTATE_COLOR;
        }
        else
        {
            joinButton.transform.Find("Join Button Text").GetComponent<TextMeshProUGUI>().text = "JOIN GAME";
            joinButton.GetComponent<Image>().color = JOIN_COLOR;
        }
    }

    private float GetStartTime()
    {
        return Networking.IsOwner(gameObject) ? ownerStartTime : nonOwnerStartTime;
    }
}
