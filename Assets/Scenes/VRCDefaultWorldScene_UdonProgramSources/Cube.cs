
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
public class Cube : UdonSharpBehaviour
{
    private bool isPickedUp = false;
    private VRCPlayerApi localPlayer;

    void Start()
    {
        localPlayer = Networking.LocalPlayer;
    }

    public override void OnPickup()
    {
        if (localPlayer != null && localPlayer == Networking.GetOwner(gameObject))
        {
            isPickedUp = true;
        }
    }

    public override void OnDrop()
    {
        if (localPlayer != null && localPlayer == Networking.GetOwner(gameObject))
        {
            isPickedUp = false;
        }
    }
}
