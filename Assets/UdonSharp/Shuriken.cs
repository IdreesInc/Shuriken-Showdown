
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Shuriken : UdonSharpBehaviour {
    void Start() {
        Debug.Log("Shuriken has been spawned.");
    }

    public override void OnPickup() {
        Debug.Log("Object has been gripped.");
    }
}
