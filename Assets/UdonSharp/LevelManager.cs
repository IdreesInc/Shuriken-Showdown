
using UdonSharp;
using UnityEngine;

public enum Level
{
    NONE,
    LOBBY,
    RUINS,
    FOUNDATIONS,
    FROZEN_BAY
}

/// <summary>
/// Client-side local UdonSharpBehaviour for switching between levels/terrains
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelManager : UdonSharpBehaviour
{

    public AudioSource lobbyMusic;
    public AudioSource battleMusic;
    public GameObject sharedTerrain;
    public GameObject lobby;
    public GameObject ruins;
    public GameObject foundations;
    public GameObject frozenBay;

    private Level loadedLevel = Level.NONE;
    private readonly Level[] levels = { Level.LOBBY, Level.RUINS, Level.FOUNDATIONS, Level.FROZEN_BAY };
    private bool musicEnabled = true;

    private void Log(string message)
    {
        Shared.Log("LevelManager", message);
    }

    private void LogError(string message)
    {
        Shared.LogError("LevelManager", message);
    }

    public static LevelManager Get()
    {
        return GameObject.Find("Level Manager").GetComponent<LevelManager>();
    }

    private GameObject GetLevelObject(Level level)
    {
        switch (level)
        {
            case Level.LOBBY:
                return lobby;
            case Level.RUINS:
                return ruins;
            case Level.FOUNDATIONS:
                return foundations;
            case Level.FROZEN_BAY:
                return frozenBay;
            default:
                LogError("Unknown level: " + level);
                return null;
        }
    }

    public static Level GetRandomLevel(Level currentLevel)
    {
        Level randomLevel;
        do
        {
            randomLevel = (Level)Random.Range(1, 5);
        } while (randomLevel == Level.LOBBY || randomLevel == currentLevel);
        return randomLevel;
    }

    public void TransitionToLevel(Level level)
    {
        if (loadedLevel == level)
        {
            return;
        }
        Log("Transitioning to level: " + level);

        if (loadedLevel == Level.LOBBY && level != Level.LOBBY)
        {
            lobbyMusic.Stop();
            if (musicEnabled)
            {
                battleMusic.Play();
            }
        }
        else if (loadedLevel != Level.LOBBY && level == Level.LOBBY)
        {
            battleMusic.Stop();
            if (musicEnabled)
            {
                lobbyMusic.Play();
            }
        }

        loadedLevel = level;

        GameObject levelObject = GetLevelObject(level);
        levelObject.SetActive(true);
        foreach (Level otherLevel in levels)
        {
            if (otherLevel != level && otherLevel != Level.LOBBY)
            {
                GetLevelObject(otherLevel).SetActive(false);
            }
        }
        Vector3 terrainPos = sharedTerrain.transform.position;
        Vector3 levelPos = levelObject.transform.position;
        // Move the shared terrain to the new level's terrain position
        sharedTerrain.transform.position = new Vector3(levelPos.x, terrainPos.y, levelPos.z);
    }

    public Vector3 GetSpawnPosition(Level level, int playerSlot)
    {
        Transform spawnMarkerParent = GetLevelObject(level).transform.Find("Spawn Markers");
        if (spawnMarkerParent == null)
        {
            LogError("Spawn markers not found for level: " + level);
            return Vector3.zero;
        }
        int spawnIndex = 0;
        if (playerSlot >= 0)
        {
            spawnIndex = playerSlot % spawnMarkerParent.childCount;
        }
        else
        {
            LogError("Player slot is not valid while trying to get spawn position: " + playerSlot);
        }
        Log("Player " + playerSlot + " spawning at index " + spawnIndex + " out of " + spawnMarkerParent.childCount + " for level " + level);
        Transform spawnMarker = spawnMarkerParent.GetChild(spawnIndex);
        return spawnMarker.position;
    }

    public Vector3[] GetPowerUpSpawnPoints(Level level)
    {
        Transform powerUpMarkerParent = GetLevelObject(level).transform.Find("Power Up Markers");
        if (powerUpMarkerParent == null)
        {
            LogError("Power up markers not found for level: " + level);
            return new Vector3[0];
        }
        Vector3[] powerUpSpawnPoints = new Vector3[powerUpMarkerParent.childCount];
        for (int i = 0; i < powerUpMarkerParent.childCount; i++)
        {
            powerUpSpawnPoints[i] = powerUpMarkerParent.GetChild(i).position;
        }
        return powerUpSpawnPoints;
    }

    public bool IsMusicEnabled()
    {
        return musicEnabled;
    }

    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;
        if (musicEnabled)
        {
            if (loadedLevel == Level.LOBBY)
            {
                lobbyMusic.Play();
            }
            else
            {
                battleMusic.Play();
            }
        }
        else
        {
            lobbyMusic.Stop();
            battleMusic.Stop();
        }
    }
}
