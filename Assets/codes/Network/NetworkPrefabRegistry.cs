using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NetworkPrefabRegistry", menuName = "Network/Prefab Registry")]
public class NetworkPrefabRegistry : ScriptableObject
{
    public const string ResourcesPath = "NetworkPrefabRegistry";

    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    public bool TryGetPrefab(string id, out GameObject prefab)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.Id == id && entry.Prefab != null)
            {
                prefab = entry.Prefab;
                return true;
            }
        }

        prefab = null;
        return false;
    }

    public bool TryGetId(GameObject prefab, out string id)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.Prefab == prefab)
            {
                id = entry.Id;
                return !string.IsNullOrWhiteSpace(id);
            }
        }

        id = null;
        return false;
    }

    public bool ContainsId(string id)
    {
        foreach (Entry entry in entries)
        {
            if (entry != null && entry.Id == id)
            {
                return true;
            }
        }

        return false;
    }

#if UNITY_EDITOR
    public List<Entry> MutableEntries => entries;
#endif

    [Serializable]
    public class Entry
    {
        public string Id;
        public GameObject Prefab;
    }
}
