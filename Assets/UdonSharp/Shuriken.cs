
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Shuriken : UdonSharpBehaviour {

    private VRCPlayerApi owner = null;
    private bool isAirborne = false;
    private float rotationSpeed = 360f * 2;

    private Vector3 gravity = new Vector3(0, -9.81f / 2, 0);

    void Start() {
        Debug.Log("Shuriken has been spawned.");
        // Reduce gravity
        GetComponent<Rigidbody>().useGravity = false;
    }

    public void SetOwner(VRCPlayerApi player) {
        owner = player;
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

    private void OnCollisionEnter(Collision collision) {
        Debug.Log("Shuriken has collided with " + collision.gameObject.name);
        isAirborne = false;
        // Determine if the object is a "Player Collider"
        if (collision.gameObject.GetComponent<PlayerCollider>() != null) {
            PlayerCollider playerCollider = collision.gameObject.GetComponent<PlayerCollider>();
            if (owner == null || playerCollider.GetPlayer() != owner) {
                string ownerName = owner == null ? "Unknown" : owner.displayName;
                Debug.Log(ownerName + "'s shuriken has hit " + playerCollider.GetPlayerName());
                Destroy(gameObject);
            }
        }
    }
}
