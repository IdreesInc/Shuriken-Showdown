
using UdonSharp;
using UnityEngine;

public enum Level {
    NONE,
    LOBBY,
    ISLAND_ONE
}

/// <summary>
/// Non-networked UdonSharpBehaviour for switching between levels/terrains
/// </summary>
public class LevelManager : UdonSharpBehaviour {

    public GameObject vrcWorldObject;
    public GameObject sharedTerrain;
    public GameObject lobby;
    public GameObject islandOne;

    private Level loadedLevel = Level.NONE;

    private void Log(string message) {
        Debug.Log("[LevelManager]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[LevelManager]: " + message);
    }
    
    public static LevelManager GetLevelManager() {
        return GameObject.Find("Level Manager").GetComponent<LevelManager>();
    }

    public void SwitchLevel(Level level) {
        if (loadedLevel == level) {
            Log("Already on level: " + level);
            return;
        }
        loadedLevel = level;
        Vector3 terrainPos = sharedTerrain.transform.position;
        switch (level) {
            case Level.LOBBY:
                Log("Switching to lobby");
                Vector3 lobbyPos = lobby.transform.position;
                sharedTerrain.transform.position = new Vector3(lobbyPos.x, terrainPos.y, lobbyPos.z);
                break;
            case Level.ISLAND_ONE:
                Log("Switching to island one");
                Vector3 islandOnePos = islandOne.transform.position;
                sharedTerrain.transform.position = new Vector3(islandOnePos.x, terrainPos.y, islandOnePos.z);
                break;
            default:
                LogError("Unknown level: " + level);
                break;
        }
        SetPlayerSpawnPoint(GetSpawnPosition(loadedLevel));
    }

    public Vector3 GetDeathPosition(Level level) {
        switch (level) {
            case Level.LOBBY:
                return GetDeathMarkerPosition(lobby);
            case Level.ISLAND_ONE:
                return GetDeathMarkerPosition(islandOne);
            default:
                LogError("Unknown level: " + level);
                return Vector3.zero;
        }
    }

    private Vector3 GetDeathMarkerPosition(GameObject parent) {
        Transform deathMarkerTrans = parent.transform.Find("Death Marker");
        if (deathMarkerTrans == null) {
            LogError("Death marker not found");
            return Vector3.zero;
        }
        GameObject deathMarker = deathMarkerTrans.gameObject;
        return deathMarker.transform.position;
    }

    public Vector3 GetSpawnPosition(Level level) {
        switch (level) {
            case Level.LOBBY:
                return GetSpawnMarkerPosition(lobby);
            case Level.ISLAND_ONE:
                return GetSpawnMarkerPosition(islandOne);
            default:
                LogError("Unknown level: " + level);
                return Vector3.zero;
        }
    }

    private Vector3 GetSpawnMarkerPosition(GameObject parent) {
        Transform spawnMarkerTrans = parent.transform.Find("Spawn Marker");
        if (spawnMarkerTrans == null) {
            LogError("Spawn marker not found");
            return Vector3.zero;
        }
        GameObject spawnMarker = spawnMarkerTrans.gameObject;
        return spawnMarker.transform.position;
    }

    public void SetPlayerSpawnPoint(Vector3 position) {
        vrcWorldObject.transform.position = position;
    }
}
