using UdonSharp;  
using UnityEngine;  
using VRC.SDKBase;  
  
public class StationTest : UdonSharpBehaviour  
{  
    public override void Interact()  
    {  
        Networking.LocalPlayer.UseAttachedStation();  
    }  
  
    public override void OnStationEntered(VRCPlayerApi player)  
    {  
        Debug.Log($"{player.displayName} Entered");  
    }  
  
    public override void OnStationExited(VRCPlayerApi player)  
    {  
        Debug.Log($"{player.displayName} Exited");  
    }  
}