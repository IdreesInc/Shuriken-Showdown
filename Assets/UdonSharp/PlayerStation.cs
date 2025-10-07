using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

[RequireComponent(typeof(VRCStation))]
public class PlayerStation : UdonSharpBehaviour
{
    private const float Y_POSITION = 10f;
    private VRCStation station;
    private Ghost ghost;

    private const float GHOST_GRAVITY = 0.3f;

    private void Log(string message)
    {
        Shared.Log("Station", message, Networking.GetOwner(gameObject));
    }

    private void LogError(string message)
    {
        Shared.LogError("Station", message, Networking.GetOwner(gameObject));
    }

    /** Udon Overrides **/

    void Start()
    {
        station = GetComponent<VRCStation>();
        if (station == null)
        {
            LogError("No VRCStation component found");
            return;
        }

        PutAway();

        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        GameObject[] playerObjects = Networking.GetPlayerObjects(Networking.GetOwner(gameObject));
        foreach (GameObject obj in playerObjects)
        {
            Ghost ghost = obj.GetComponent<Ghost>();
            if (ghost != null)
            {
                this.ghost = ghost;
                break;
            }
        }
        if (ghost == null)
        {
            LogError("No Ghost found for player");
            return;
        }
    }

    void Update()
    {
        // Update location for all players
        Vector3 playerPosition = Networking.GetOwner(gameObject).GetPosition();
        playerPosition.y = Y_POSITION;
        station.transform.position = playerPosition;
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
        if (Networking.IsOwner(gameObject))
        {
            Networking.GetOwner(gameObject).SetGravityStrength(GHOST_GRAVITY);
            ghost.FollowPlayer();
        }
    }

    public override void OnStationExited(VRCPlayerApi player)
    {
        Log("OnStationExited: " + player.displayName);
        if (Networking.IsOwner(gameObject))
        {
            Networking.GetOwner(gameObject).SetGravityStrength(1f);
            ghost.StopFollowing();
        }
    }

    /** Custom Methods **/

    public void MoveToPosition(Vector3 position)
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Non-owner tried to move station");
            return;
        }
        Log("Moving station to " + position);
        station.transform.position = position;
    }

    public void SeatPlayer()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Non-owner tried to seat player");
            return;
        }
        Log("Seating player");
        station.PlayerMobility = VRCStation.Mobility.Mobile;
        station.UseStation(Networking.GetOwner(gameObject));
    }

    public void ResetLocation()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Non-owner tried to reset station");
            return;
        }
        Log("Resetting station position");
        PutAway();
        ghost.StopFollowing();
    }

    private void PutAway()
    {
        station.transform.position = new Vector3(10f * Networking.GetOwner(gameObject).playerId, Y_POSITION, 0);
    }
}