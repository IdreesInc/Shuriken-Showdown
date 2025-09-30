using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[UnityEngine.RequireComponent(typeof(VRCStation))]
public class PlayerStation : UdonSharpBehaviour
{
    private VRCStation station;

    private void Log(string message)
    {
        Shared.Log("Station", message, Networking.GetOwner(gameObject));
    }

    private void LogError(string message)
    {
        Shared.LogError("Station", message, Networking.GetOwner(gameObject));
    }

    void Start()
    {
        station = GetComponent<VRCStation>();

        int playerId = Networking.GetOwner(gameObject).playerId;
        if (playerId == -1)
        {
            LogError("No owner assigned to station somehow");
        }
        Vector3 position = station.transform.position;
        position.x += 10f * playerId;
        station.transform.position = position;
    }

    public override void Interact()
    {
        Log("Interacted");
        station.PlayerMobility = VRCStation.Mobility.Mobile;
        station.UseStation(Networking.LocalPlayer);
    }
    public override void OnStationEntered(VRCPlayerApi player)
    {
        Log("OnStationEntered: " + player.displayName);
        if (!player.isLocal)
        {
            station.PlayerMobility = VRCStation.Mobility.Immobilize;
            Log("Set to immobile");
            var pos = station.transform.position;
            pos.y -= 20f;
            station.transform.position = pos;
        }
    }

}