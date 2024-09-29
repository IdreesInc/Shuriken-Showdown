using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Shuriken : UdonSharpBehaviour {

    public AudioSource audioSource;
    
    private VRCPlayerApi owner = null;
    private bool isHeld = false;
    private bool hasBeenThrown = false;
    private float rotationSpeed = 360f * 2;
    private float maxDistanceFromOwner = 75;

    private Vector3 gravity = new Vector3(0, -9.81f / 2, 0);
    private Color[] colors = { Color.gray, Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };


    void Start() {
        Debug.Log("Shuriken has been spawned.");
        // Reduce gravity
        GetComponent<Rigidbody>().useGravity = false;
    }

    public void SetOwner(VRCPlayerApi player) {
        owner = player;
        // Get the player index and set the color
        int playerIndex = player.playerId % colors.Length;
        GetComponent<Renderer>().material.color = colors[playerIndex];
    }

    public void ReturnToOwner() {
        if (owner == null) {
            Debug.LogError("Shuriken: Owner is not set");
            return;
        }
        Debug.Log("Returning shuriken to " + owner.displayName);
        // Place the shuriken in front of the player
        transform.position = owner.GetPosition() + owner.GetRotation() * new Vector3(0, 0.2f, 1);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        hasBeenThrown = false;
    }

    void FixedUpdate() {
        float velocity = GetComponent<Rigidbody>().velocity.magnitude;
        if (!isHeld) {
            if (velocity > 0.3f) {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            } else if (owner != null) {
                if (velocity < 0.01f && hasBeenThrown && Vector3.Distance(transform.position, owner.GetPosition()) > 10) {
                    // Shuriken at rest after being thrown
                    ReturnToOwner();
                } else if (!hasBeenThrown && Vector3.Distance(transform.position, owner.GetPosition()) > 5) {
                    // Shuriken has not been thrown yet, make sure it is within reach
                    ReturnToOwner();
                }
            }
        }
        // If the shuriken is too far from the owner, return it no matter what
        if (owner != null && Vector3.Distance(transform.position, owner.GetPosition()) > maxDistanceFromOwner) {
            ReturnToOwner();
        }
        GetComponent<Rigidbody>().AddForce(gravity, ForceMode.Acceleration);
    }

    public override void OnPickup() {
        Debug.Log("Object has been gripped");
        isHeld = true;
        // Disable collision with anything
        GetComponent<Rigidbody>().detectCollisions = false;
        if (Networking.LocalPlayer != owner) {
            Debug.Log("Shuriken owned by " + owner.displayName + " has been picked up by " + Networking.LocalPlayer.displayName);
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
            if (owner == null || playerCollider.GetPlayer() != owner) {
                string ownerName = owner == null ? "Unknown" : owner.displayName;
                Debug.Log(ownerName + "'s shuriken has hit " + playerCollider.GetPlayerName());
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
