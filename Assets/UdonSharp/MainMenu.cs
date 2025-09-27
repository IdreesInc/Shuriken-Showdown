
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;
using UnityEngine.UI;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class MainMenu : UdonSharpBehaviour
{

    public GameObject[] playerIcons;
    public GameObject[] gameMasterTexts;
    public GameObject settingsUI;

    /** Udon Overrides */

    private void Log(string message)
    {
        Shared.Log("MainMenu", message, Networking.GetOwner(gameObject));
    }

    private void LogError(string message)
    {
        Shared.LogError("MainMenu", message, Networking.GetOwner(gameObject));
    }

    void Update()
    {
        // TODO: Only update when necessary
        UpdateGameMaster();
        UpdatePlayerIcons();
    }

    public void OnStartGamePressed()
    {
        Log("Start Game Pressed");
        GameLogic.Get().SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GameLogic.StartGame));
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
}
