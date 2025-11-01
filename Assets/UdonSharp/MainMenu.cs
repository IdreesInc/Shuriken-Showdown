
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
    public Button musicToggle;

    /// <summary>
    /// Delay in seconds before non-owners can start the game
    /// </summary>
    private const int NON_OWNER_DELAY = 20;
    /// <summary>
    /// Delay in seconds before owners can restart the game
    /// </summary>
    private const int OWNER_DELAY = 5;
    private const int MIN_PLAYER_COUNT = 2;
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

    public void OnToggleMusicPressed()
    {
        Log("Toggle Music Pressed");
        LevelManager levelManager = LevelManager.Get();
        if (levelManager.IsMusicEnabled())
        {
            levelManager.SetMusicEnabled(false);
            musicToggle.transform.Find("Music Toggle Text").GetComponent<TextMeshProUGUI>().text = "MUSIC OFF";
            musicToggle.GetComponent<Image>().color = Shared.HexToColor("#F52222");
        }
        else
        {
            levelManager.SetMusicEnabled(true);
            musicToggle.transform.Find("Music Toggle Text").GetComponent<TextMeshProUGUI>().text = "MUSIC ON";
            musicToggle.GetComponent<Image>().color = Shared.HexToColor("#2FD93C");
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
        } else if (gameLogic.GetJoinedPlayerSlotCount() < MIN_PLAYER_COUNT)
        {
            timerText.text = "Waiting for more players to join (" + gameLogic.GetJoinedPlayerSlotCount() + "/" + MIN_PLAYER_COUNT + ")";
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
        if (gameLogic.GetJoinedPlayerSlotCount() < MIN_PLAYER_COUNT)
        {
            // Can't start the game if not enough players have joined
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
