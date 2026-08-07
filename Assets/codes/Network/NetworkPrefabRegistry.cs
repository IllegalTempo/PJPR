using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NetworkPrefabRegistry", menuName = "Game/Network Prefab Registry")]
public class NetworkPrefabRegistry : ScriptableObject
{
    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    public bool TryGetDefinition(string prefabId, out PrefabDefinition prefabDefinition)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.PrefabId == prefabId)
            {
                prefabDefinition = entry.PrefabDefinition;
                return prefabDefinition != null;
            }
        }

        prefabDefinition = null;
        return false;
    }

    public bool TryGetPrefabId(PrefabDefinition prefabDefinition, out string prefabId)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.PrefabDefinition == prefabDefinition)
            {
                prefabId = entry.PrefabId;
                return !string.IsNullOrWhiteSpace(prefabId);
            }
        }

        prefabId = null;
        return false;
    }

    [Serializable]
    public class Entry
    {
        public string PrefabId;
        public PrefabDefinition PrefabDefinition;
    }
}
