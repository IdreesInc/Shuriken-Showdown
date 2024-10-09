using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Miner28.UdonUtils.Network;
using System;

public class Shuriken : NetworkInterface {

    public AudioSource audioSource;
    
    private const float ROTATION_SPEED = 360f * 2;
    private const float MAX_DISTANCE = 75;
    private const float THROW_FORCE = 5;
    private readonly Vector3 GRAVITY_FORCE = new Vector3(0, -9.81f / 3, 0);

    [UdonSynced] private int playerId = -1;
    [UdonSynced] private int playerNumber = -1;
    [UdonSynced] private bool isHeld = false;
    [UdonSynced] private bool hasBeenThrown = false;

    [UdonSynced] private readonly int[] powerUps = { -1, -1, -1 };
    [UdonSynced] private int score = 0;

    private VRCPlayerApi Player {
        get {
            if (playerId == -1) {
                return null;
            }
            return VRCPlayerApi.GetPlayerById(playerId);
        }
    }

    private string GetPlayerName() {
        if (Player == null) {
            return "[Unnamed Player]";
        }
        return Player.displayName;
    }

    private void Log(string message) {
        Debug.Log("[Shuriken - " + playerId + "]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[Shuriken - " + playerId + "]: " + message);
    }

    void Start() {
        if (playerId == -1) {
            LogError("playerId is not set");
        }
        if (playerNumber == -1) {
            LogError("playerNumber is not set");
        }
        Log("Shuriken has been spawned.");
    }

    public void SetPlayerId(int playerId) {
        Log("Setting owner id to " + playerId);
        this.playerId = playerId;
        UpdateOwnership();
    }

    public void SetPlayerNumber(int playerNumber) {
        Log("Setting player number to " + playerNumber);
        this.playerNumber = playerNumber;
    }

    public int GetPlayerId() {
        return playerId;
    }

    public int GetPlayerNumber() {
        return playerNumber;
    }

    public int GetScore() {
        return score;
    }

    public override void OnDeserialization() {
        // Log("Deserializing shuriken with owner id " + playerId);
        ApplyPowerUpEffects();
        UpdateOwnership();
    }

    /// <summary>
    /// Triggered over networking by PowerUp when it detects a collision with this shuriken
    /// </summary>
    [NetworkedMethod]
    public void ActivatePowerUp(int type) {
        AddPowerUp(type);
    }

    private void AddPowerUp(int type) {
        Log("Adding power up: " + PowerUp.GetPowerUpName(type));
        ReturnToPlayer();
        // Move every power up down one slot and add the new power up to the first slot
        for (int i = powerUps.Length - 1; i > 0; i--) {
            powerUps[i] = powerUps[i - 1];
        }
        powerUps[0] = type;
        ApplyPowerUpEffects();
        if (Networking.LocalPlayer.playerId == playerId) {
            GameLogic.GetLocalGameLogic().OnPowerUpCollected(type, powerUps);
        }
    }

    private void ApplyPowerUpEffects() {
        ResetPowerUpEffects();
        for (int i = 0; i < powerUps.Length; i++) {
            if (powerUps[i] != -1) {
                ApplyPowerUp(powerUps[i]);
            }
        }
    }

    private void ApplyPowerUp(int type) {
        Log("Applying power up: " + PowerUp.GetPowerUpName(type));
        if (type == 0) {
            // Embiggen
            transform.localScale = new Vector3(
                transform.localScale.x + 1,
                transform.localScale.y + 1,
                transform.localScale.z + 1
            );
        }
    }

    private void ResetPowerUpEffects() {
        // Reset all effects of power ups
        transform.localScale = new Vector3(1, 1, 1);
    }

    private void UpdateOwnership() {
        if (playerId == Networking.LocalPlayer.playerId && !Networking.IsOwner(gameObject)) {
            Log("Claiming network ownership of shuriken");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            ReturnToPlayer();
        }
        GetComponent<Renderer>().material.color = Shared.ShurikenColors()[playerId % Shared.Colors().Length];
    }

    public bool HasPlayer() {
        return Player != null;
    }

    private Vector3 GetSpawnOffset() {
        int numOfEmbiggens = 0;
        for (int i = 0; i < powerUps.Length; i++) {
            if (powerUps[i] == 0) {
                numOfEmbiggens++;
            }
        }
        return new Vector3(0, 0.5f, 1f + 0.5f * numOfEmbiggens);
    }

    /// <summary>
    /// Return the shuriken to the player who owns it
    /// </summary>
    public void ReturnToPlayer() {
        if (!HasPlayer()) {
            LogError("Owner is not set");
            return;
        } else if (Networking.LocalPlayer.playerId != playerId) {
            // Log("Won't return as shuriken is not owned by " + Networking.LocalPlayer.playerId);
            return;
        }
        Log("Returning shuriken to " + playerId);
        // Place the shuriken in front of the player
        PutInFrontOfPlayer();
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        hasBeenThrown = false;
    }

    private void PutInFrontOfPlayer() {
        if (!HasPlayer()) {
            LogError("Owner is not set");
            return;
        }
        // Place the shuriken in front of the player
        transform.position = Player.GetPosition() + Player.GetRotation() * GetSpawnOffset();
    }

    void FixedUpdate() {
        GetComponent<Rigidbody>().useGravity = false;
        if (!Networking.IsOwner(gameObject)) {
            // TODO: Might be able to remove this in the future
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
        if (hasBeenThrown) {
            GetComponent<Rigidbody>().AddForce(GRAVITY_FORCE, ForceMode.Acceleration);
        }
        if (!hasBeenThrown && !isHeld) {
            // Freeze the shuriken in place
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            PutInFrontOfPlayer();
            // Set collision layer to PickupNoEnvironment
            gameObject.layer = 14;
            // Spin that baby
            transform.Rotate(Vector3.up, ROTATION_SPEED / 2 * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        } else {
            // Set collision layer to Pickup
            gameObject.layer = 13;
        }
    }

    public override void OnPickup() {
        Log("Object has been gripped");
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
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
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        isHeld = false;
        hasBeenThrown = true;
        // Enable collision with anything
        GetComponent<Rigidbody>().detectCollisions = true;
        // Throw the shuriken if the initial velocity is high enough
        if (GetComponent<Rigidbody>().velocity.magnitude > 1) {
            Log("Velocity: " + GetComponent<Rigidbody>().velocity.magnitude + ", throwing shuriken");
            GetComponent<Rigidbody>().AddForce(Player.GetRotation() * Vector3.forward * THROW_FORCE, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (!Networking.IsOwner(gameObject)) {
            Log("Not the owner, skipping collision");
            return;
        }

        if (collision.gameObject.GetComponent<Shuriken>() != null) {
            // Collided with another shuriken
            Log("Shuriken has collided with another shuriken");
            // Return the shuriken to the owner
            ReturnToPlayer();
        } else {
            Log("Shuriken has collided with " + collision.gameObject.name);
        }
    }

    private void OnTriggerEnter(Collider collider) {
        if (!Networking.IsOwner(gameObject)) {
            Log("Not the owner, skipping collision");
            return;
        }
        
        if (collider.gameObject.GetComponent<PlayerCollider>() != null) {
            PlayerCollider playerCollider = collider.gameObject.GetComponent<PlayerCollider>();
            if (!HasPlayer() || playerCollider.GetPlayer() != playerId) {
                Log(playerId + "'s shuriken has hit " + playerCollider.GetPlayer());
                // Notify the player
                playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnHit), SyncTarget.All, GetPlayerName(), playerNumber);
                // Play hit sound
                if (audioSource != null) {
                    audioSource.Play();
                }
                // Return the shuriken to the owner
                ReturnToPlayer();
                // Increase the score
                score++;
            }
        } else {
            Log("Shuriken has triggered with " + collider.gameObject.name);
        }
    }
}
