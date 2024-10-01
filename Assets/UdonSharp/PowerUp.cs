
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PowerUp : UdonSharpBehaviour {

    private void Log(string message) {
        Debug.Log("[PowerUp]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[PowerUp]: " + message);
    }
    
    void Start() {
        Log("Power up has been spawned");
    }

    private void OnCollisionEnter(Collision collision) {
        Log("Power up has collided with " + collision.gameObject.name);
        // Determine if the object is a "Player Collider"
        if (collision.gameObject.GetComponent<PlayerCollider>() != null) {
            PlayerCollider playerCollider = collision.gameObject.GetComponent<PlayerCollider>();
            Log(playerCollider.GetPlayerName() + " has picked up a power up");
        }
        // Move 0.5 units to the right
        transform.position = new Vector3(transform.position.x + 0.5f, transform.position.y, transform.position.z);
    }

}
