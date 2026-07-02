using System;
using System.Collections.Generic;
using UnityEngine;

[Obsolete("Network prefab lookup is generated at runtime from ItemDefinition assets under Resources/Prefabs. Use NetworkSystem.TryGetNetworkPrefab instead.")]
public class NetworkPrefabRegistry : ScriptableObject
{
    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;

    [Serializable]
    public class Entry
    {
        public string Id;
        public GameObject Prefab;
    }
}
