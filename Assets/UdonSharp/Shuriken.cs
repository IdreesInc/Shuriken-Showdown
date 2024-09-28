
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Shuriken : UdonSharpBehaviour {
    private bool isAirborne = false;
    private float rotationSpeed = 360f * 2;

    private Vector3 gravity = new Vector3(0, -9.81f / 2, 0);

    void Start() {
        Debug.Log("Shuriken has been spawned.");
        // Reduce gravity
        GetComponent<Rigidbody>().useGravity = false;
    }

    void FixedUpdate() {
        if (isAirborne) {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
        GetComponent<Rigidbody>().AddForce(gravity, ForceMode.Acceleration);
    }

    public override void OnPickup() {
        Debug.Log("Object has been gripped.");
        isAirborne = false;
    }

    public override void OnDrop() {
        Debug.Log("Object has been dropped");
        isAirborne = true;
    }

    public override void OnPlayerCollisionEnter(VRCPlayerApi player) {
        Debug.Log("Shuriken has collided with a player with name: " + player.displayName);
        isAirborne = false;
    }

    private void OnCollisionEnter(Collision collision) {
        Debug.Log("Shuriken has collided with something.");
        isAirborne = false;
    }
}
