
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class LevelManager : UdonSharpBehaviour {

    public GameObject sharedTerrain;
    public GameObject lobby;

    void Start() {
        Vector3 terrainPos = sharedTerrain.transform.position;
        Vector3 lobbyPos = lobby.transform.position;
        sharedTerrain.transform.position = new Vector3(lobbyPos.x, terrainPos.y, lobbyPos.z);
    }
}
