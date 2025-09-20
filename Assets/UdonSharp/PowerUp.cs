
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PowerUp : UdonSharpBehaviour
{

    [UdonSynced] private int powerUpType = 0;

    public static string GetPowerUpName(int type)
    {
        // Can't make this static due to UdonSharp limitations, using getter instead
        string[] POWER_UP_NAMES = {
            "Embiggen",
            "Amphetamines",
            "Moon Shoes",
            "Badaboom"
        };
        if (type < 0 || type >= POWER_UP_NAMES.Length)
        {
            return "Unknown";
        }
        return POWER_UP_NAMES[type];
    }

    public static string GetPowerUpSubtitle(int type)
    {
        string[] POWER_UP_SUBTITLES = {
            "Go big or go home",
            "Gotta go fast",
            "Reach for the stars",
            "Hearing protection recommended"
        };
        if (type < 0 || type >= POWER_UP_SUBTITLES.Length)
        {
            return "Unknown";
        }
        return POWER_UP_SUBTITLES[type];
    }

    public static int GetNumberOfPowerUps()
    {
        // Stupid necessity due to UdonSharp not allowing for static fields
        return 4;
    }

    private void Log(string message)
    {
        Debug.Log("[PowerUp]: " + message);
    }

    private void LogError(string message)
    {
        Debug.LogError("[PowerUp]: " + message);
    }

    void Start()
    {
        Log("Power up has been spawned");
    }

    public void SetPowerUpType(int powerUpType)
    {
        Log("Setting power up type to " + powerUpType);
        this.powerUpType = powerUpType;
    }

    public void SetRandomPowerUpType()
    {
        int randomPowerUpType = Random.Range(0, GetNumberOfPowerUps());
        SetPowerUpType(randomPowerUpType);
    }

    public int GetPowerUpType()
    {
        return powerUpType;
    }

    private void FixedUpdate()
    {
        transform.Rotate(0, 1, 0);
        float yOffset = Mathf.Sin(Time.time * 2f) * 0.005f;
        transform.position = new Vector3(transform.position.x, transform.position.y + yOffset, transform.position.z);
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Power up has triggered with " + collider.gameObject.name);
        if (collider.gameObject.GetComponent<Shuriken>() != null)
        {
            Shuriken shuriken = collider.gameObject.GetComponent<Shuriken>();
            Log("Power up has collided with a shuriken owned by " + Networking.GetOwner(shuriken.gameObject).displayName);
            // shuriken.SendMethodNetworked(nameof(Shuriken.ActivatePowerUp), SyncTarget.All, powerUpType);
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(shuriken.ActivatePowerUp), powerUpType);
            GameLogic.Get().OnPowerUpCollected(gameObject);
        }
    }

}
