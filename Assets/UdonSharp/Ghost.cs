
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class Ghost : UdonSharpBehaviour
{
    private readonly Vector3 OFFSET = new Vector3(0, 0.8f, 0);

    private bool following;

    private void Log(string message)
    {
        Shared.Log("Ghost", message, Networking.GetOwner(gameObject));
    }

    private void LogError(string message)
    {
        Shared.LogError("Ghost", message, Networking.GetOwner(gameObject));
    }

    void Start()
    {
    }

    void Update()
    {
        if (following && Networking.IsOwner(gameObject))
        {
            VRCPlayerApi owner = Networking.GetOwner(gameObject);
            if (owner != null)
            {
                Vector3 ownerPosition = owner.GetPosition();
                Quaternion ownerRotation = owner.GetRotation();
                transform.SetPositionAndRotation(ownerPosition + OFFSET, ownerRotation);

            }
        }
    }

    /** Custom Methods **/

    public void FollowPlayer()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Cannot follow player when not owner");
            return;
        }
        Log("Following player");
        following = true;
    }

    public void StopFollowing()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Cannot stop following player when not owner");
            return;
        }
        Log("Stopped following player");
        following = false;
        PutAway();
    }

    private void PutAway()
    {
        if (!Networking.IsOwner(gameObject))
        {
            LogError("Cannot put away when not owner");
            return;
        }
        transform.position = new Vector3(0, -20f, 0);
    }
}
