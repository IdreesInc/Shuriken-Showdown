
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
    private PlayerStation playerStation;

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
            PlayerStation station = obj.GetComponent<PlayerStation>();
            if (station != null)
            {
                playerStation = station;
            }
        }
    }

    public void PickLevel()
    {
        if (!Shared.IsAdmin(Networking.LocalPlayer))
        {
            Log("Not an admin, cannot pick level");
            return;
        }
        int level = levelSelector.value;
        Log($"Picking level {level}");
        LevelManager.Get().TransitionToLevel((Level)level);
        playerCollider.GoToLevelSpawn((Level)level);
    }

    public void GivePowerUp()
    {
        if (!Shared.IsAdmin(Networking.LocalPlayer))
        {
            Log("Not an admin, cannot give power up");
            return;
        }
        if (powerUpSelector.value == 0)
        {
            return;
        }
        int powerUp = powerUpSelector.value - 1;
        Log($"Giving power up {powerUp}");
        shuriken.ActivatePowerUp(powerUp);
        powerUpSelector.value = 0;
    }

    public void GoInvisible()
    {
        if (!Shared.IsAdmin(Networking.LocalPlayer))
        {
            Log("Not an admin, cannot go invisible");
            return;
        }
        playerStation.GoInvisible();
        shuriken.SetActive(false);
        shuriken.enabled = false;
    }

    public void GoVisible()
    {
        if (!Shared.IsAdmin(Networking.LocalPlayer))
        {
            Log("Not an admin, cannot go visible");
            return;
        }
        playerStation.GoVisible();
        shuriken.enabled = true;
        shuriken.SetActive(true);
    }
}
