
using UdonSharp;
using UnityEngine;

public enum Level {
    NONE,
    LOBBY,
    RUINS,
    FOUNDATIONS
}

/// <summary>
/// Local UdonSharpBehaviour for switching between levels/terrains
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelManager : UdonSharpBehaviour {
    public GameObject sharedTerrain;
    public GameObject afterlife;
    public GameObject lobby;
    public GameObject ruins;
    public GameObject foundations;

    private Level loadedLevel = Level.NONE;
    private readonly Level[] levels = {Level.LOBBY, Level.RUINS, Level.FOUNDATIONS};

    private void Log(string message) {
        Debug.Log("[LevelManager]: " + message);
    }

    private void LogError(string message) {
        Debug.LogError("[LevelManager]: " + message);
    }
    
    public static LevelManager Get() {
        return GameObject.Find("Level Manager").GetComponent<LevelManager>();
    }

    private GameObject GetLevelObject(Level level) {
        switch (level) {
            case Level.LOBBY:
                return lobby;
            case Level.RUINS:
                return ruins;
            case Level.FOUNDATIONS:
                return foundations;
            default:
                LogError("Unknown level: " + level);
                return null;
        }
    }

    public static Level GetRandomLevel(Level currentLevel) {
        Level randomLevel;
        do {
            randomLevel = (Level) Random.Range(1, 4);
        } while (randomLevel == Level.LOBBY || randomLevel == currentLevel);
        return randomLevel;
    }

    public void TransitionToLevel(Level level) {
        if (loadedLevel == level) {
            return;
        }
        Log("Transitioning to level: " + level);
        if (loadedLevel != Level.NONE) {
            // Stop the current background music
            GetBackgroundMusic(loadedLevel).Stop();
        }

        loadedLevel = level;

        GameObject levelObject = GetLevelObject(level);
        levelObject.SetActive(true);
        foreach (Level otherLevel in levels) {
            if (otherLevel != level && otherLevel != Level.LOBBY) {
                GetLevelObject(otherLevel).SetActive(false);
            }
        }
        Vector3 terrainPos = sharedTerrain.transform.position;
        Vector3 levelPos = levelObject.transform.position;
        // Move the shared terrain to the new level's terrain position
        sharedTerrain.transform.position = new Vector3(levelPos.x, terrainPos.y, levelPos.z);
        
        // Play the new background music
        GetBackgroundMusic(loadedLevel).Play();
    }

    public Vector3 GetSpawnPosition(Level level, int playerNumber) {
        Transform spawnMarkerParent = GetLevelObject(level).transform.Find("Spawn Markers");
        if (spawnMarkerParent == null) {
            LogError("Spawn markers not found for level: " + level);
            return Vector3.zero;
        }
        int spawnIndex = 0;
        if (playerNumber != -1) {
            spawnIndex = (playerNumber - 1) % spawnMarkerParent.childCount;
        } else {
            LogError("Player number is -1 while trying to get spawn position");
        }
        Log("Player " + playerNumber + " spawning at index " + spawnIndex + " out of " + spawnMarkerParent.childCount + " for level " + level);
        Transform spawnMarker = spawnMarkerParent.GetChild(spawnIndex);
        return spawnMarker.position;
    }

    public Vector3 GetDeathPosition() {
        Transform deathMarkerTrans = afterlife.transform.Find("Death Marker");
        if (deathMarkerTrans == null) {
            LogError("Death marker not found");
            return Vector3.zero;
        }
        GameObject deathMarker = deathMarkerTrans.gameObject;
        Vector3 position = deathMarker.transform.position;
        return position;
    }

    public Vector3[] GetPowerUpSpawnPoints(Level level) {
        Transform powerUpMarkerParent = GetLevelObject(level).transform.Find("Power Up Markers");
        if (powerUpMarkerParent == null) {
            LogError("Power up markers not found for level: " + level);
            return new Vector3[0];
        }
        Vector3[] powerUpSpawnPoints = new Vector3[powerUpMarkerParent.childCount];
        for (int i = 0; i < powerUpMarkerParent.childCount; i++) {
            powerUpSpawnPoints[i] = powerUpMarkerParent.GetChild(i).position;
        }
        return powerUpSpawnPoints;
    }

    private AudioSource GetBackgroundMusic(Level level) {
        return GetLevelObject(level).transform.Find("Background Music").GetComponent<AudioSource>();
    }
}
