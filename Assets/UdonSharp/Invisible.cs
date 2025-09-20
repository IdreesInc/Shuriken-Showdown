
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Invisible : UdonSharpBehaviour
{
    void Start()
    {
        // Disable renderer
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }
}
