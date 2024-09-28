
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Shuriken : UdonSharpBehaviour {
    private bool isAirborne = false;
    private float rotationSpeed = 360f * 2;

    void Start() {
        Debug.Log("Shuriken has been spawned.");
    }

    void Update() {
        if (isAirborne) {
            transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    public override void OnPickup() {
        Debug.Log("Object has been gripped.");
        isAirborne = false;
    }

    public override void OnDrop() {
        Debug.Log("Object has been dropped");
        isAirborne = true;
    }

    private void OnCollisionEnter(Collision collision) {
        Debug.Log("Shuriken has collided with something.");
        isAirborne = false;
    }
}
