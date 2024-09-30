using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Shuriken : UdonSharpBehaviour {

    public AudioSource audioSource;
    
    private const float ROTATION_SPEED = 360f * 2;
    private const float MAX_DISTANCE = 75;
    private readonly Vector3 GRAVITY_FORCE = new Vector3(0, -9.81f / 2, 0);
    private readonly Color[] COLORS = { Color.gray, Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };

    [UdonSynced] private int ownerId = -1;
    [UdonSynced] private bool isHeld = false;
    [UdonSynced] private bool hasBeenThrown = false;

    void Start() {
        Debug.Log("Shuriken has been spawned.");
        // Reduce gravity
        GetComponent<Rigidbody>().useGravity = false;
    }

    public void SetOwner(int playerId) {
        ownerId = playerId;
        GetComponent<Renderer>().material.color = COLORS[ownerId];
    }

    private VRCPlayerApi Owner {
        get {
            if (ownerId == -1) {
                return null;
            }
            return VRCPlayerApi.GetPlayerById(ownerId);
        }
    }

    public bool HasOwner() {
        return Owner != null;
    }

    public void ReturnToOwner() {
        if (!HasOwner()) {
            Debug.LogError("Shuriken: Owner is not set");
            return;
        }
        Debug.Log("Returning shuriken to " + ownerId);
        // Place the shuriken in front of the player
        transform.position = Owner.GetPosition() + Owner.GetRotation() * new Vector3(0, 0.2f, 1);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        hasBeenThrown = false;
    }

    void FixedUpdate() {
        float velocity = GetComponent<Rigidbody>().velocity.magnitude;
        if (!isHeld) {
            if (velocity > 0.3f) {
                transform.Rotate(Vector3.up, ROTATION_SPEED * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            } else if (HasOwner()) {
                if (velocity < 0.01f && hasBeenThrown && Vector3.Distance(transform.position, Owner.GetPosition()) > 10) {
                    // Shuriken at rest after being thrown
                    ReturnToOwner();
                } else if (!hasBeenThrown && Vector3.Distance(transform.position, Owner.GetPosition()) > 5) {
                    // Shuriken has not been thrown yet, make sure it is within reach
                    ReturnToOwner();
                }
            }
        }
        // If the shuriken is too far from the owner, return it no matter what
        if (HasOwner()) {
            if (Vector3.Distance(transform.position, Owner.GetPosition()) > MAX_DISTANCE) {
                ReturnToOwner();
            } else if (transform.position.y < -10) {
                // Shuriken fell below map
                ReturnToOwner();
            }
        }
        GetComponent<Rigidbody>().AddForce(GRAVITY_FORCE, ForceMode.Acceleration);
    }

    public override void OnPickup() {
        Debug.Log("Object has been gripped");
        isHeld = true;
        // Disable collision with anything
        GetComponent<Rigidbody>().detectCollisions = false;
        if (Networking.LocalPlayer.playerId != ownerId) {
            Debug.Log("Shuriken owned by " + ownerId + " has been picked up by " + Networking.LocalPlayer.playerId);
            ReturnToOwner();
        }
    }

    public override void OnDrop() {
        Debug.Log("Object has been released");
        isHeld = false;
        hasBeenThrown = true;
        // Enable collision with anything
        GetComponent<Rigidbody>().detectCollisions = true;
    }

    private void OnCollisionEnter(Collision collision) {
        // Determine if the object is a "Player Collider"
        if (collision.gameObject.GetComponent<PlayerCollider>() != null) {
            PlayerCollider playerCollider = collision.gameObject.GetComponent<PlayerCollider>();
            if (!HasOwner() || playerCollider.GetPlayer() != ownerId) {
                Debug.Log(ownerId + "'s shuriken has hit " + playerCollider.GetPlayer());
                // Play hit sound
                if (audioSource != null) {
                    audioSource.Play();
                }
                // Return the shuriken to the owner
                ReturnToOwner();
            }
        } else {
            Debug.Log("Shuriken has collided with " + collision.gameObject.name);
        }
    }
}
