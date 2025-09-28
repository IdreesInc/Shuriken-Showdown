using UdonSharp;
using VRC.SDKBase;

[UnityEngine.RequireComponent(typeof(VRCStation))]
public class StationTest : UdonSharpBehaviour
{
    private VRCStation station;

    private void Log(string message)
    {
        Shared.Log("Station", message);
    }

    void Start()
    {
        station = GetComponent<VRCStation>();
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
        }
    }

}