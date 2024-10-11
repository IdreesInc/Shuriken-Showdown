
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Miner28.UdonUtils.Network;

public class PowerUp : NetworkInterface {

    [UdonSynced] private int powerUpType = 0;

    public static string GetPowerUpName(int type) {
        // Can't make this static due to UdonSharp limitations, using getter instead
        string[] POWER_UP_NAMES = {
            "Embiggen",
        };
        if (type < 0 || type >= POWER_UP_NAMES.Length) {
            return "Unknown";
        }
        return POWER_UP_NAMES[type];
    }

    public static string GetPowerUpSubtitle(int type) {
        string[] POWER_UP_SUBTITLES = {
            "Go big or go home",
        };
        if (type < 0 || type >= POWER_UP_SUBTITLES.Length) {
            return "Unknown";
        }
        return POWER_UP_SUBTITLES[type];
    }

    public static int GetNumberOfPowerUps() {
        // Stupid necessity due to UdonSharp not allowing for static fields
        return 1;
    }

    private void Log(string message) {
        Debug.Log("[PowerUp]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[PowerUp]: " + message);
    }
    
    void Start() {
        Log("Power up has been spawned");
    }

    public void SetPowerUpType(int powerUpType) {
        Log("Setting power up type to " + powerUpType);
        this.powerUpType = powerUpType;
    }

    public void SetRandomPowerUpType() {
        int randomPowerUpType = Random.Range(0, GetNumberOfPowerUps());
        SetPowerUpType(randomPowerUpType);
    }

    public int GetPowerUpType() {
        return powerUpType;
    }

    private void FixedUpdate() {
        transform.Rotate(0, 1, 0);
        float yOffset = Mathf.Sin(Time.time * 2f) * 0.005f;
        transform.position = new Vector3(transform.position.x, transform.position.y + yOffset, transform.position.z);
    }

    private void OnTriggerEnter(Collider collider) {
        if (!Networking.IsOwner(gameObject)) {
            Log("Not the owner, skipping collision");
            return;
        }
        Log("Power up has triggered with " + collider.gameObject.name);
        if (collider.gameObject.GetComponent<Shuriken>() != null) {
            Shuriken shuriken = collider.gameObject.GetComponent<Shuriken>();
            Log("Power up has collided with a shuriken owned by " + shuriken.GetPlayerId());
            shuriken.SendMethodNetworked(nameof(Shuriken.ActivatePowerUp), SyncTarget.All, powerUpType);
            GameLogic.GetGameLogic().OnPowerUpCollected(gameObject);
        }
    }

}
