using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using Miner28.UdonUtils.Network;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class Shuriken : NetworkInterface {

    public AudioSource audioSource;
    
    private const float ROTATION_SPEED = 360f * 2;
    private const float MAX_DISTANCE = 75;
    private const float MAX_GROUND_DISTANCE = 5;
    private const float THROW_FORCE = 5;
    private const float DEFAULT_WALK_SPEED = 3f;
    private const float DEFAULT_RUN_SPEED = 5f;
    private const float DEFAULT_JUMP_FORCE = 3.5f;
    private readonly Vector3 GRAVITY_FORCE = new Vector3(0, -9.81f / 3, 0);
    private const float EMBIGGEN_MOD = 1f;
    private const float AMPHETAMINES_MOD = 3.5f;
    private const float MOON_SHOES_MOD = 2.75f;

    [UdonSynced] private int playerId = -1;
    [UdonSynced] private int playerNumber = -1;

    /// <summary>
    /// Whether we are in-game, which determines whether the shuriken can be used
    /// and if it can collide with anything
    /// </summary>
    [UdonSynced] private bool inGame = false;
    [UdonSynced] private bool isHeld = false;
    [UdonSynced] private bool hasBeenThrown = false;
    [UdonSynced] private int powerUpOne = -1;
    [UdonSynced] private int powerUpTwo = -1;
    [UdonSynced] private int powerUpThree = -1;
    [UdonSynced] private int score = 0;

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

    /** Udon Overrides **/

    void Start() {
        if (playerId == -1) {
            LogError("playerId is not set");
        }
        if (playerNumber == -1) {
            LogError("playerNumber is not set");
        }
        Log("Shuriken has been spawned.");
    }

    public override void OnDeserialization() {
        // Log("Deserializing shuriken with owner id " + playerId);
        ApplyPowerUpEffects();
        UpdateOwnership();
    }

    void FixedUpdate() {
        if (!inGame) {
            // Disable the ability to pick up the shuriken
            GetComponent<Rigidbody>().detectCollisions = false;
        } else {
            // Enable the ability to pick up the shuriken
            GetComponent<Rigidbody>().detectCollisions = true;
        }

        GetComponent<Rigidbody>().useGravity = false;
        float velocity = GetComponent<Rigidbody>().velocity.magnitude;
        if (!isHeld) {
            if (velocity > 0.3f) {
                transform.Rotate(Vector3.up, ROTATION_SPEED * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            } else if (HasPlayer() && Networking.IsOwner(gameObject)) {
                if (velocity < 0.01f && hasBeenThrown && Vector3.Distance(transform.position, Player.GetPosition()) > MAX_GROUND_DISTANCE) {
                    // Shuriken at rest after being thrown
                    ReturnToPlayer();
                } else if (!hasBeenThrown && Vector3.Distance(transform.position, Player.GetPosition()) > 5) {
                    // Shuriken has not been thrown yet, make sure it is within reach
                    ReturnToPlayer();
                }
            }
        }
        // If the shuriken is too far from the owner, return it no matter what
        if (HasPlayer() && Networking.IsOwner(gameObject)) {
            if (Vector3.Distance(transform.position, Player.GetPosition()) > MAX_DISTANCE) {
                ReturnToPlayer();
            } else if (transform.position.y < -1) {
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
            // Set collision layer to Walkthrough
            // TODO: Determine if this has any adverse effects
            gameObject.layer = 17;
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
        if (!inGame) {
            Log("Shuriken is disabled, ignoring collision");
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
            Log("Not the owner, skipping trigger");
            return;
        }
        if (!inGame) {
            Log("Shuriken is disabled, ignoring trigger");
            return;
        }
        if (collider.gameObject.GetComponent<PlayerCollider>() != null && (isHeld || hasBeenThrown)) {
            PlayerCollider playerCollider = collider.gameObject.GetComponent<PlayerCollider>();
            if (!HasPlayer() || playerCollider.GetPlayer() != playerId) {
                Log(playerId + "'s shuriken has hit " + playerCollider.GetPlayer());
                if (playerCollider.IsAlive()) {
                    // Notify the player
                    playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnHit), SyncTarget.All, GetPlayerName(), playerNumber);
                    // Play hit sound
                    if (audioSource != null) {
                        audioSource.Play();
                    }
                    // Increase the score
                    score++;
                } else {
                    Log("Player is already dead, ignoring");
                }
                // Return the shuriken to the owner
                ReturnToPlayer();
            }
        } else {
            Log("Shuriken has triggered with " + collider.gameObject.name);
        }
    }

    /** Event Handlers **/

    /// <summary>
    /// Triggered over the network by PowerUp when it detects a collision with this shuriken
    /// </summary>
    [NetworkedMethod]
    public void ActivatePowerUp(int type) {
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        AddPowerUp(type);
    }

    [NetworkedMethod]
    public void OnRoundStart() {
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        inGame = true;
    }

    [NetworkedMethod]
    public void OnRoundEnd() {
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        ResetShurikenBetweenRounds();
        inGame = false;
    }

    [NetworkedMethod]
    public void OnGameEnd() {
        if (!Networking.IsOwner(gameObject)) {
            return;
        }
        ResetShurikenBetweenRounds();
        powerUpOne = -1;
        powerUpTwo = -1;
        powerUpThree = -1;
        score = 0;
        inGame = false;
        ResetPowerUpEffects();
    }

    /** Custom Methods **/

    public void SetPlayerId(int playerId) {
        Log("Setting owner id to " + playerId);
        this.playerId = playerId;
        UpdateOwnership();
    }

    public void SetPlayerNumber(int playerNumber) {
        Log("Setting player number to " + playerNumber);
        this.playerNumber = playerNumber;
    }

    public void ReturnToPlayer() {
        if (!HasPlayer()) {
            LogError("Unable to return to player, player is not set");
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

    public bool HasPlayer() {
        return Player != null;
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

    private string GetPlayerName() {
        if (Player == null) {
            return "[Unnamed Player]";
        }
        return Player.displayName;
    }

    private void ResetShurikenBetweenRounds() {
        ReturnToPlayer();
    }

    private void AddPowerUp(int type) {
        Log("Adding power up: " + PowerUp.GetPowerUpName(type));
        ReturnToPlayer();
        powerUpThree = powerUpTwo;
        powerUpTwo = powerUpOne;
        powerUpOne = type;
        ApplyPowerUpEffects();
        if (Networking.LocalPlayer.playerId == playerId) {
            LocalPlayerLogic.Get().ShowEquippedUI(type, powerUpOne, powerUpTwo, powerUpThree);
        }
    }

    private void ApplyPowerUpEffects() {
        ResetPowerUpEffects();
        ApplyPowerUp(powerUpOne);
        ApplyPowerUp(powerUpTwo);
        ApplyPowerUp(powerUpThree);
    }

    private void ApplyPowerUp(int type) {
        if (type == -1) {
            return;
        }
        VRCPlayerApi player = Player;
        if (player == null) {
            LogError("Player is null while attempting to apply power up");
            return;
        }
        Log("Applying power up: " + PowerUp.GetPowerUpName(type));
        if (type == 0) {
            // Embiggen
            transform.localScale = new Vector3(
                transform.localScale.x + EMBIGGEN_MOD,
                transform.localScale.y + EMBIGGEN_MOD,
                transform.localScale.z + EMBIGGEN_MOD
            );
        } else if (type == 1) {
            // Amphetamines
            if (Networking.IsOwner(gameObject)) {
                player.SetWalkSpeed(player.GetWalkSpeed() + AMPHETAMINES_MOD);
                player.SetRunSpeed(player.GetRunSpeed() + AMPHETAMINES_MOD);
            }
        } else if (type == 2) {
            // Moon Shoes
            if (Networking.IsOwner(gameObject)) {
                player.SetJumpImpulse(player.GetJumpImpulse() + MOON_SHOES_MOD);
            }
        }
    }

    private void ResetPowerUpEffects() {
        // Reset all effects of power ups
        transform.localScale = new Vector3(1, 1, 1);
        if (Networking.IsOwner(gameObject)) {
            VRCPlayerApi player = Player;
            if (player == null) {
                LogError("Player is null while attempting to reset power up effects");
            } else {
                player.SetWalkSpeed(DEFAULT_WALK_SPEED);
                player.SetRunSpeed(DEFAULT_RUN_SPEED);
                player.SetJumpImpulse(DEFAULT_JUMP_FORCE);
            }
        }
    }

    private void UpdateOwnership() {
        if (playerId == Networking.LocalPlayer.playerId && !Networking.IsOwner(gameObject)) {
            Log("Claiming network ownership of shuriken");
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            ReturnToPlayer();
        }
        if (playerId == -1) {
            GetComponent<Renderer>().material.color = Color.grey;
        } else {
            GetComponent<Renderer>().material.color = Shared.Colors()[(playerId - 1) % Shared.Colors().Length];
        }
    }

    private Vector3 GetSpawnOffset() {
        int numOfEmbiggens = 0;
        if (powerUpOne == 0) {
            numOfEmbiggens++;
        }
        if (powerUpTwo == 0) {
            numOfEmbiggens++;
        }
        if (powerUpThree == 0) {
            numOfEmbiggens++;
        }
        return new Vector3(0, 0.5f, 1f + 0.5f * numOfEmbiggens);
    }

    private void PutInFrontOfPlayer() {
        if (!HasPlayer()) {
            LogError("Unable to place in front of player, player is not set");
            return;
        }
        // Place the shuriken in front of the player
        transform.position = Player.GetPosition() + Player.GetRotation() * GetSpawnOffset();
    }
}
