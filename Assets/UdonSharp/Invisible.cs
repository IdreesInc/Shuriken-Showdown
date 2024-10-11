
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Invisible : UdonSharpBehaviour
{
    void Start() {
        // Disable renderer
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }
}
