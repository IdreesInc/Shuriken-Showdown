
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public enum PowerUpType
{
    Embiggen = 0,
    Amphetamines = 1,
    MoonShoes = 2,
    Badaboom = 3,
    Jumpman = 4,
    HomingPigeon = 5
}

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class PowerUp : UdonSharpBehaviour
{

    [UdonSynced] private PowerUpType powerUpType = PowerUpType.Embiggen;

    public static string GetPowerUpName(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Embiggen:
                return "Embiggen";
            case PowerUpType.Amphetamines:
                return "Amphetamines";
            case PowerUpType.MoonShoes:
                return "Moon Shoes";
            case PowerUpType.Badaboom:
                return "Badaboom";
            case PowerUpType.Jumpman:
                return "Jumpman";
            case PowerUpType.HomingPigeon:
                return "Homing Pigeon";
            default:
                return "N/A";
        }
    }

    public static string GetPowerUpSubtitle(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Embiggen:
                return "Go big or go home";
            case PowerUpType.Amphetamines:
                return "Gotta go fast";
            case PowerUpType.MoonShoes:
                return "Reach for the stars";
            case PowerUpType.Badaboom:
                return "Hearing protection recommended";
            case PowerUpType.Jumpman:
                return "It's-a me, legally-distinct character!";
            case PowerUpType.HomingPigeon:
                return "You miss all the shots you don't take";
            default:
                return "N/A";
        }
    }

    public static string GetPowerUpName(int type)
    {
        if (type < 0 || type > 4)
        {
            return "N/A";
        }
        return GetPowerUpName((PowerUpType)type);
    }

    public static string GetPowerUpSubtitle(int type)
    {
        if (type < 0 || type > 4)
        {
            return "N/A";
        }
        return GetPowerUpSubtitle((PowerUpType)type);
    }

    public static int GetNumberOfPowerUps()
    {
        // Stupid necessity due to UdonSharp not allowing for static fields
        return 5;
    }

    private void Log(string message)
    {
        Shared.Log("PowerUp", message, Networking.GetOwner(gameObject));
    }

    private void LogError(string message)
    {
        Shared.LogError("PowerUp", message, Networking.GetOwner(gameObject));
    }

    void Start()
    {
        Log("Power up has been spawned");
    }

    public void SetPowerUpType(PowerUpType powerUpType)
    {
        Log("Setting power up type to " + powerUpType);
        this.powerUpType = powerUpType;
    }

    public void SetPowerUpType(int powerUpType)
    {
        Log("Setting power up type to " + powerUpType);
        this.powerUpType = (PowerUpType)powerUpType;
    }

    public void SetRandomPowerUpType()
    {
        int randomPowerUpType = Random.Range(0, GetNumberOfPowerUps());
        SetPowerUpType(randomPowerUpType);
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
            shuriken.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(shuriken.ActivatePowerUp), (int)powerUpType);
            GameLogic.Get().OnPowerUpCollected(gameObject);
        }
    }

}
