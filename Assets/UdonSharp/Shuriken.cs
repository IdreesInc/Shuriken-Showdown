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

    [UdonSynced] private int playerId = -1;
    [UdonSynced] private bool isHeld = false;
    [UdonSynced] private bool hasBeenThrown = false;

    private VRCPlayerApi Player {
        get {
            if (playerId == -1) {
                return null;
            }
            return VRCPlayerApi.GetPlayerById(playerId);
        }
    }

    private void Log(string message) {
        Debug.Log("[Shuriken - " + playerId + "]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[Shuriken - " + playerId + "]: " + message);
    }

    void Start() {
        Log("Shuriken has been spawned.");
    }

    public void SetPlayerId(int playerId) {
        Log("Setting owner id to " + playerId);
        this.playerId = playerId;
        UpdateOwnership();
    }

    public override void OnDeserialization() {
        Log("Deserializing shuriken with owner id " + playerId);
        UpdateOwnership();
    }

    public void UpdateOwnership() {
        if (playerId == Networking.LocalPlayer.playerId && !Networking.IsOwner(gameObject)) {
            Log("Claiming network ownership of shuriken");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            ReturnToPlayer();
        }
        GetComponent<Renderer>().material.color = COLORS[playerId];
    }

    public bool HasPlayer() {
        return Player != null;
    }

    public void ReturnToPlayer() {
        if (!HasPlayer()) {
            LogError("Owner is not set");
            return;
        } else if (Networking.LocalPlayer.playerId != playerId) {
            Log("Won't return as shuriken is not owned by " + Networking.LocalPlayer.playerId);
            return;
        }
        Log("Returning shuriken to " + playerId);
        // Place the shuriken in front of the player
        transform.position = Player.GetPosition() + Player.GetRotation() * new Vector3(0, 0.2f, 1);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        hasBeenThrown = false;
    }

    void FixedUpdate() {
        GetComponent<Rigidbody>().useGravity = false;
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        float velocity = GetComponent<Rigidbody>().velocity.magnitude;
        if (!isHeld) {
            if (velocity > 0.3f) {
                transform.Rotate(Vector3.up, ROTATION_SPEED * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            } else if (HasPlayer()) {
                if (velocity < 0.01f && hasBeenThrown && Vector3.Distance(transform.position, Player.GetPosition()) > 10) {
                    // Shuriken at rest after being thrown
                    ReturnToPlayer();
                } else if (!hasBeenThrown && Vector3.Distance(transform.position, Player.GetPosition()) > 5) {
                    // Shuriken has not been thrown yet, make sure it is within reach
                    ReturnToPlayer();
                }
            }
        }
        // If the shuriken is too far from the owner, return it no matter what
        if (HasPlayer()) {
            if (Vector3.Distance(transform.position, Player.GetPosition()) > MAX_DISTANCE) {
                ReturnToPlayer();
            } else if (transform.position.y < -10) {
                // Shuriken fell below map
                ReturnToPlayer();
            }
        }
        GetComponent<Rigidbody>().AddForce(GRAVITY_FORCE, ForceMode.Acceleration);
    }

    public override void OnPickup() {
        Log("Object has been gripped");
        isHeld = true;
        // Disable collision with anything
        GetComponent<Rigidbody>().detectCollisions = false;
        if (Networking.LocalPlayer.playerId != playerId) {
            Log("Shuriken owned by " + playerId + " has been picked up by " + Networking.LocalPlayer.playerId);
            ReturnToPlayer();
        }
    }

    public override void OnDrop() {
        Log("Object has been released");
        isHeld = false;
        hasBeenThrown = true;
        // Enable collision with anything
        GetComponent<Rigidbody>().detectCollisions = true;
    }

    private void OnCollisionEnter(Collision collision) {
        // Determine if the object is a "Player Collider"
        if (collision.gameObject.GetComponent<PlayerCollider>() != null) {
            PlayerCollider playerCollider = collision.gameObject.GetComponent<PlayerCollider>();
            if (!HasPlayer() || playerCollider.GetPlayer() != playerId) {
                Log(playerId + "'s shuriken has hit " + playerCollider.GetPlayer());
                // Play hit sound
                if (audioSource != null) {
                    audioSource.Play();
                }
                // Return the shuriken to the owner
                ReturnToPlayer();
            }
        } else {
            Log("Shuriken has collided with " + collision.gameObject.name);
        }
    }
}
