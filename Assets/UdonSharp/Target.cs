
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.UdonNetworkCalling;
using VRC.Udon;

public class Target : UdonSharpBehaviour
{

    public GameObject[] targetMarkers;

    [UdonSynced] private int currentMarkerIndex = -1;

    private void Log(string message)
    {
        Shared.Log("Target", message, Networking.GetOwner(gameObject));
    }

    private void LogError(string message)
    {
        Shared.LogError("Target", message, Networking.GetOwner(gameObject));
    }

    void Start()
    {
        if (Networking.IsOwner(gameObject))
        {
            MoveToRandomMarker();
        }
    }

    void FixedUpdate()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        // Spin the target
        transform.Rotate(Vector3.forward, -90f * Time.deltaTime);
    }

    [NetworkCallable]
    public void OnHit()
    {
        if (!Networking.IsOwner(gameObject))
        {
            return;
        }
        Log("Target hit registered, moving to new marker");
        MoveToRandomMarker();
    }

    public void PlaySound()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    private void MoveToRandomMarker()
    {
        if (targetMarkers.Length == 0)
        {
            LogError("No target markers set, cannot move to random marker");
            return;
        }
        int randomIndex = Random.Range(0, targetMarkers.Length);
        while (randomIndex == currentMarkerIndex && targetMarkers.Length > 1)
        {
            randomIndex = Random.Range(0, targetMarkers.Length);
        }
        currentMarkerIndex = randomIndex;
        Transform markerTransform = targetMarkers[randomIndex].transform;
        transform.position = markerTransform.position;
        Log("Moved to target marker " + randomIndex);
    }

}
