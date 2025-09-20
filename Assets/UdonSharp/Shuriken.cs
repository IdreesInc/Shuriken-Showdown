using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using System;
using VRC.SDK3.UdonNetworkCalling;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TrailRenderer))]
public class Shuriken : UdonSharpBehaviour
{

    public AudioSource audioSource;

    private const float ROTATION_SPEED = 360f * 3;
    private const float IDLE_ROTATION_SPEED = 90f;
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

    // [UdonSynced] private int playerId = -1;
    // [UdonSynced] private int playerNumber = -1;

    /// <summary>
    /// Whether we are in-game, which determines whether the shuriken can be used
    /// and if it can collide with anything
    /// </summary>
    [UdonSynced] private bool inGame = true;
    [UdonSynced] private bool isHeld = false;
    [UdonSynced] private bool hasBeenThrown = false;
    [UdonSynced] private bool hasFirstContact = false;
    [UdonSynced] private int powerUpOne = -1;
    [UdonSynced] private int powerUpTwo = -1;
    [UdonSynced] private int powerUpThree = -1;
    [UdonSynced] private int score = 0;
    [UdonSynced] private Quaternion rotationOnThrow = Quaternion.identity;

    private VRCPlayerApi Player
    {
        get
        {
            // if (playerId == -1) {
            //     return null;
            // }
            // return VRCPlayerApi.GetPlayerById(playerId);
            return Networking.GetOwner(gameObject);
        }
    }

    private void Log(string message)
    {
        Debug.Log("[Shuriken - Slot: " + LoggingName() + "]: " + message);
    }

    private void LogError(string message)
    {
        Debug.LogError("[Shuriken - Slot: " + LoggingName() + "]: " + message);
    }

    private string LoggingName()
    {
        string name = Player.displayName == null || Player.displayName == "" ? "Unnamed Player" : Player.displayName;
        return "(" + name + " - Slot: " + GameLogic.Get().GetPlayerSlot(Player.playerId) + ")";
    }

    /** Udon Overrides **/

    void Start()
    {
        Log("Shuriken has been spawned.");
        UpdateColor();
    }

    public override void OnDeserialization()
    {
        // Log("Deserializing shuriken with owner id " + playerId);
        ApplyPowerUpEffects();
        UpdateColor();
        // UpdateOwnership();
    }

    void Update()
    {
        // Update the trail graphics (applies to all clients)
        if (GetComponent<TrailRenderer>() != null)
        {
            GetComponent<TrailRenderer>().emitting = hasBeenThrown;
        }
    }

    void FixedUpdate()
    {
        if (!inGame)
        {
            // Disable the ability to pick up the shuriken
            GetComponent<Rigidbody>().detectCollisions = false;
        }
        else
        {
            // Enable the ability to pick up the shuriken
            GetComponent<Rigidbody>().detectCollisions = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();

        GetComponent<Rigidbody>().useGravity = false;
        float velocity = GetComponent<Rigidbody>().velocity.magnitude;
        if (!isHeld)
        {
            if (velocity > 0.3f)
            {
                if (!hasFirstContact)
                {
                    transform.rotation = rotationOnThrow;
                    transform.Rotate(Vector3.up, ROTATION_SPEED * Time.deltaTime);
                    rotationOnThrow = transform.rotation;
                }
                else
                {
                    transform.Rotate(Vector3.up, ROTATION_SPEED * Time.deltaTime);
                    transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
                }
            }
            else if (Networking.IsOwner(gameObject))
            {
                // TODO: Wtf is this nested if statement, clean it up
                if (velocity < 0.01f && hasBeenThrown && Vector3.Distance(transform.position, Player.GetPosition()) > MAX_GROUND_DISTANCE)
                {
                    // Shuriken at rest after being thrown
                    ReturnToPlayer();
                }
                else if (!hasBeenThrown && Vector3.Distance(transform.position, Player.GetPosition()) > 5)
                {
                    // Shuriken has not been thrown yet, make sure it is within reach
                    ReturnToPlayer();
                }
            }
        }
        // If the shuriken is too far from the owner, return it no matter what
        if (Networking.IsOwner(gameObject))
        {
            if (Vector3.Distance(transform.position, Player.GetPosition()) > MAX_DISTANCE)
            {
                ReturnToPlayer();
            }
            else if (transform.position.y < -1)
            {
                // Shuriken fell below map
                ReturnToPlayer();
            }
        }

        if (hasBeenThrown)
        {
            rb.AddForce(GRAVITY_FORCE, ForceMode.Acceleration);
        }

        if (!hasBeenThrown && !isHeld)
        {
            // Freeze the shuriken in place
            GetComponent<Rigidbody>().velocity = Vector3.zero;
            PutInFrontOfPlayer();
            // Set collision layer to Walkthrough
            // TODO: Determine if this has any adverse effects
            gameObject.layer = 17;
            if (inGame)
            {
                // Spin that baby
                transform.Rotate(Vector3.up, IDLE_ROTATION_SPEED * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
            }
            else
            {
                // Set rotation to player's rotation
                Vector3 playerRotation = Player.GetRotation().eulerAngles;
                transform.rotation = Quaternion.Euler(0, playerRotation.y, 0);
            }
        }
        else
        {
            // Set collision layer to Pickup
            gameObject.layer = 13;
        }
    }

    public override void OnPickup()
    {
        Log("Object has been gripped");
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        isHeld = true;
        hasBeenThrown = false;
        hasFirstContact = false;
        // Disable collision with anything
        GetComponent<Rigidbody>().detectCollisions = false;
        // if (Networking.LocalPlayer.playerId != playerId) {
        //     Log("Shuriken owned by " + playerId + " has been picked up by " + Networking.LocalPlayer.playerId);
        //     ReturnToPlayer();
        // }
    }

    public override void OnDrop()
    {
        Log("Object has been released");
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        isHeld = false;
        hasBeenThrown = true;
        hasFirstContact = false;
        // Enable collision with anything
        GetComponent<Rigidbody>().detectCollisions = true;
        // Throw the shuriken if the initial velocity is high enough
        if (GetComponent<Rigidbody>().velocity.magnitude > 1)
        {
            Log("Velocity: " + GetComponent<Rigidbody>().velocity.magnitude + ", throwing shuriken");
            GetComponent<Rigidbody>().AddForce(Player.GetRotation() * Vector3.forward * THROW_FORCE, ForceMode.Impulse);
        }
        if (Player != null && !Player.IsUserInVR())
        {
            // Non-VR player, fix shuriken rotation
            transform.rotation = Quaternion.Euler(0, Player.GetRotation().eulerAngles.y, 0);
        }
        rotationOnThrow = transform.rotation;
    }

    private void CheckForLocalExplosionCollision(Vector3 position, int level)
    {
        Vector3 playerPosition = Player.GetPosition();
        float range = 0;
        if (level == 1)
        {
            range = 3.75f;
        }
        else if (level == 2)
        {
            range = 4.5f;
        }
        else if (level >= 3)
        {
            range = 5.25f;
        }
        // Determine if the local player is within the explosion radius
        if (Vector3.Distance(playerPosition, position) <= range)
        {
            Log("Local player has been hit by explosion");
        }
        else
        {
            Log("Distance: " + Vector3.Distance(playerPosition, position));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasBeenThrown)
        {
            // Create explosions locally
            int explosionLevel = GetPowerUpLevel(3);
            if (explosionLevel > 0)
            {
                Effects.Get().SpawnExplosion(collision.contacts[0].point, explosionLevel + 1);
                CheckForLocalExplosionCollision(collision.contacts[0].point, explosionLevel + 1);
            }
        }

        if (!Networking.IsOwner(gameObject))
        {
            Log("Not the owner, skipping collision");
            return;
        }
        if (!inGame)
        {
            Log("Shuriken is disabled, ignoring collision");
            return;
        }

        hasFirstContact = true;

        if (collision.gameObject.GetComponent<Shuriken>() != null)
        {
            // Collided with another shuriken
            Log("Shuriken has collided with another shuriken");
            // Return the shuriken to the owner
            ReturnToPlayer();
        }
        else
        {
            Log("Shuriken has collided with " + collision.gameObject.name);
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!Networking.IsOwner(gameObject))
        {
            Log("Not the owner, skipping trigger");
            return;
        }
        if (!inGame)
        {
            Log("Shuriken is disabled, ignoring trigger");
            return;
        }

        if (collider.gameObject.GetComponent<PlayerCollider>() != null && (isHeld || hasBeenThrown))
        {
            PlayerCollider playerCollider = collider.gameObject.GetComponent<PlayerCollider>();
            VRCPlayerApi opponentPlayer = Networking.GetOwner(collider.gameObject);
            if (!Networking.IsOwner(collider.gameObject))
            {
                Log(Player.playerId + "'s shuriken has hit " + opponentPlayer.displayName);
                if (GameLogic.Get().IsPlayerAlive(opponentPlayer.playerId))
                {
                    // Notify the player
                    // playerCollider.SendMethodNetworked(nameof(PlayerCollider.OnHit), SyncTarget.All, GetPlayerName(), playerNumber);
                    int playerSlot = GameLogic.Get().GetPlayerSlot(Player.playerId);
                    playerCollider.SendCustomNetworkEvent(NetworkEventTarget.All, nameof(playerCollider.OnHit), GetPlayerName(), playerSlot);
                    // Cache the hit locally
                    // playerCollider.hasBeenHitLocally = true;
                    // Play hit sound
                    if (audioSource != null)
                    {
                        audioSource.Play();
                    }
                    // Increase the score
                    score++;
                    // Show UI
                    LocalPlayerLogic playerLogic = LocalPlayerLogic.Get();
                    playerLogic.ShowKillUI(GameLogic.Get().GetPlayerSlot(opponentPlayer.playerId), opponentPlayer.displayName);
                }
                else
                {
                    Log("Player is already dead, ignoring");
                }
                // Return the shuriken to the owner
                ReturnToPlayer();
            }
        }
        else
        {
            Log("Shuriken has triggered with " + collider.gameObject.name);
        }
    }

    /** Event Handlers **/

    /// <summary>
    /// Triggered over the network by PowerUp when it detects a collision with this shuriken
    /// </summary>
    [NetworkCallable]
    public void ActivatePowerUp(int type)
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        AddPowerUp(type);
    }

    [NetworkCallable]
    public void OnRoundStart()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
    }

    [NetworkCallable]
    public void OnFightingStart()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Fighting has started, enabling shuriken");
        inGame = true;
    }

    [NetworkCallable]
    public void OnRoundEnd()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        ResetShurikenBetweenRounds();
        inGame = false;
    }

    [NetworkCallable]
    public void OnGameEnd()
    {
        if (!Networking.IsOwner(gameObject))
        {
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

    public int GetScore()
    {
        return score;
    }

    private void ReturnToPlayer()
    {
        Log("Returning shuriken to " + Player.displayName);
        // Place the shuriken in front of the player
        PutInFrontOfPlayer();
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        hasBeenThrown = false;
        hasFirstContact = false;
    }

    private string GetPlayerName()
    {
        if (Player == null)
        {
            return "[Unnamed Player]";
        }
        return Player.displayName;
    }

    private void ResetShurikenBetweenRounds()
    {
        ReturnToPlayer();
    }

    private void AddPowerUp(int type)
    {
        Log("Adding power up: " + PowerUp.GetPowerUpName(type));
        ReturnToPlayer();
        powerUpThree = powerUpTwo;
        powerUpTwo = powerUpOne;
        powerUpOne = type;
        ApplyPowerUpEffects();
        if (Networking.IsOwner(gameObject))
        {
            LocalPlayerLogic.Get().ShowEquippedUI(type, powerUpOne, powerUpTwo, powerUpThree);
        }
    }

    private void ApplyPowerUpEffects()
    {
        ResetPowerUpEffects();
        ApplyPowerUp(powerUpOne);
        ApplyPowerUp(powerUpTwo);
        ApplyPowerUp(powerUpThree);
    }

    private void ApplyPowerUp(int type)
    {
        if (type == -1)
        {
            return;
        }
        VRCPlayerApi player = Player;
        if (player == null)
        {
            LogError("Player is null while attempting to apply power up");
            return;
        }
        Log("Applying power up: " + PowerUp.GetPowerUpName(type));
        if (type == 0)
        {
            // Embiggen
            transform.localScale = new Vector3(
                transform.localScale.x + EMBIGGEN_MOD,
                transform.localScale.y + EMBIGGEN_MOD,
                transform.localScale.z + EMBIGGEN_MOD
            );
        }
        else if (type == 1)
        {
            // Amphetamines
            if (Networking.IsOwner(gameObject))
            {
                player.SetWalkSpeed(player.GetWalkSpeed() + AMPHETAMINES_MOD);
                player.SetRunSpeed(player.GetRunSpeed() + AMPHETAMINES_MOD);
            }
        }
        else if (type == 2)
        {
            // Moon Shoes
            if (Networking.IsOwner(gameObject))
            {
                player.SetJumpImpulse(player.GetJumpImpulse() + MOON_SHOES_MOD);
            }
        }
    }

    private void ResetPowerUpEffects()
    {
        // Reset all effects of power ups
        transform.localScale = new Vector3(1, 1, 1);
        if (Networking.IsOwner(gameObject))
        {
            VRCPlayerApi player = Player;
            if (player == null)
            {
                LogError("Player is null while attempting to reset power up effects");
            }
            else
            {
                player.SetWalkSpeed(DEFAULT_WALK_SPEED);
                player.SetRunSpeed(DEFAULT_RUN_SPEED);
                player.SetJumpImpulse(DEFAULT_JUMP_FORCE);
            }
        }
    }

    // private void UpdateOwnership() {
    //     if (playerId == Networking.LocalPlayer.playerId && !Networking.IsOwner(gameObject)) {
    //         Log("Claiming network ownership of shuriken");
    //         Networking.SetOwner(Networking.LocalPlayer, gameObject);
    //         ReturnToPlayer();
    //     }
    //     Color color = Color.grey;
    //     if (playerId != -1) {
    //         color = Shared.Colors()[(playerId - 1) % Shared.Colors().Length];
    //     }
    //     GetComponent<Renderer>().material.color = color;
    //     if (GetComponent<TrailRenderer>() != null) {
    //         GetComponent<TrailRenderer>().endColor = color;
    //     }
    // }

    private void UpdateColor()
    {
        Log("Updating shuriken color for player slot " + GameLogic.Get().GetPlayerSlot(Player.playerId));
        Color color = Color.grey;
        int playerSlot = GameLogic.Get().GetPlayerSlot(Player.playerId);
        if (playerSlot != -1)
        {
            color = Shared.Colors()[playerSlot % Shared.Colors().Length];
        }
        GetComponent<Renderer>().material.color = color;
        if (GetComponent<TrailRenderer>() != null)
        {
            GetComponent<TrailRenderer>().endColor = color;
        }
    }

    private Vector3 GetSpawnOffset()
    {
        int numOfEmbiggens = GetPowerUpLevel(0);
        return new Vector3(0, 0.5f, 1f + 0.5f * numOfEmbiggens);
    }

    private int GetPowerUpLevel(int type)
    {
        int level = 0;
        if (powerUpOne == type)
        {
            level++;
        }
        if (powerUpTwo == type)
        {
            level++;
        }
        if (powerUpThree == type)
        {
            level++;
        }
        return level;
    }

    private void PutInFrontOfPlayer()
    {
        // Place the shuriken in front of the player
        transform.position = Player.GetPosition() + Player.GetRotation() * GetSpawnOffset();
    }
}
