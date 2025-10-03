
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

public class DebugUI : UdonSharpBehaviour
{

    public TMP_Dropdown levelSelector;
    public TMP_Dropdown powerUpSelector;

    private PlayerCollider playerCollider;
    private Shuriken shuriken;

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
            }
            Shuriken shuriken = obj.GetComponent<Shuriken>();
            if (shuriken != null)
            {
                this.shuriken = shuriken;
            }
        }
    }

    public void PickLevel()
    {
        int level = levelSelector.value;
        Log($"Picking level {level}");
        LevelManager.Get().TransitionToLevel((Level)level);
        playerCollider.GoToLevelSpawn((Level)level);
    }

    public void GivePowerUp()
    {
        if (powerUpSelector.value == 0)
        {
            return;
        }
        int powerUp = powerUpSelector.value - 1;
        Log($"Giving power up {powerUp}");
        shuriken.ActivatePowerUp(powerUp);
        powerUpSelector.value = 0;
    }
}
