
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Shuriken : UdonSharpBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        // Debug.Log("Collided with: " + collision.gameObject.name);
    }
}
