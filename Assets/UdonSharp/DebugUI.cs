
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class DebugUI : UdonSharpBehaviour
{

    public TMP_Dropdown dropdown;

    private PlayerCollider playerCollider;

    private void Log(string message)
    {
        Shared.Log("DebugUI", message);
    }

    void Start()
    {
        GameObject[] playerObjects = Networking.GetPlayerObjects(Networking.LocalPlayer);
        foreach (GameObject obj in playerObjects)
        {
            PlayerCollider collider = obj.GetComponent<PlayerCollider>();
            if (collider != null)
            {
                playerCollider = collider;
                break;
            }
        }
    }

    public void PickLevel()
    {
        int level = dropdown.value;
        Log($"Picking level {level}");
        LevelManager.Get().TransitionToLevel((Level)level);
        playerCollider.GoToLevelSpawn((Level)level);
    }
}
