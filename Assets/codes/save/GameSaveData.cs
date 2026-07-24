using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public List<PlayerData> PlayerLocations = new List<PlayerData>(); //Player data is updated upon player join
    public List<NetworkObjectSnapshot> NetworkObjects = new List<NetworkObjectSnapshot>();
    public List<SlotSnapshot> SlotRelationships = new List<SlotSnapshot>();

    public GameSaveData()
    {
    }

    public GameSaveData(List<PlayerData> playerDatas, List<NetworkObjectSnapshot> networkObjectSnapshots, List<SlotSnapshot> slotSnapshots)
    {
        PlayerLocations = playerDatas ?? new List<PlayerData>();
        NetworkObjects = networkObjectSnapshots ?? new List<NetworkObjectSnapshot>();
        SlotRelationships = slotSnapshots ?? new List<SlotSnapshot>();
    }
}


[Serializable]
public class PlayerData
{
    public string SteamID;
    public Vector3 Position;
    public Quaternion Rotation;

    public PlayerData()
    {
    }

    public PlayerData(string steamID, Vector3 position, Quaternion rotation)
    {
        SteamID = steamID;
        Position = position;
        Rotation = rotation;
    }
}
[Serializable]
public struct NetworkObjectSnapshot
{
    public string Uid;
    public ulong Owner;
    public string PrefabId;
    public Vector3 Position;
    public Quaternion Rotation;

    public NetworkObjectSnapshot(string uid, ulong owner, string prefabId, Vector3 position, Quaternion rotation)
    {
        Uid = uid;
        Owner = owner;
        PrefabId = prefabId;
        Position = position;
        Rotation = rotation;
    }
    public static List<NetworkObjectSnapshot> GetNetworkPrefabSnapshotInScene()
    {
        List<NetworkObjectSnapshot> snapshots = new List<NetworkObjectSnapshot>();
        foreach (NetworkPrefabIdentity networkObject in NetworkPrefabIdentity.GetNetworkPrefabInScene())
        {
            snapshots.Add(networkObject.GetSnapshot());
        }
        return snapshots;
    }
}
[Serializable]
public struct SlotSnapshot
{
    public string SlotId;
    public string AttachedItemId;
    public Quaternion rotation;

    public SlotSnapshot(string slotId, string attachedItemId, Quaternion rotation)
    {
        SlotId = slotId;
        AttachedItemId = attachedItemId;
        this.rotation = rotation;
    }
}
