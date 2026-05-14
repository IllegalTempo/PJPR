using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public List<InstalledModuleSaveData> InstalledModules = new List<InstalledModuleSaveData>();
    public List<PlayerLocationSaveData> PlayerLocations = new List<PlayerLocationSaveData>();
}

[Serializable]
public class InstalledModuleSaveData
{
    public int Slot;
    public string PrefabID;

    public InstalledModuleSaveData()
    {
    }

    public InstalledModuleSaveData(int slot, string prefabID)
    {
        Slot = slot;
        PrefabID = prefabID;
    }
}

[Serializable]
public class PlayerLocationSaveData
{
    public string SteamID;
    public Vector3 Position;
    public Quaternion Rotation;

    public PlayerLocationSaveData()
    {
    }

    public PlayerLocationSaveData(string steamID, Vector3 position, Quaternion rotation)
    {
        SteamID = steamID;
        Position = position;
        Rotation = rotation;
    }
}
