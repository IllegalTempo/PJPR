using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class NetworkPrefabRegistryWindow : EditorWindow
{
    private const string RegistryAssetPath = "Assets/Resources/NetworkPrefabRegistry.asset";
    private const string PrefabRoot = "Assets/Resources/Prefabs";

    private NetworkPrefabRegistry _registry;
    private Vector2 _scroll;

    [MenuItem("Tools/Network Objects/Prefab Registry")]
    public static void ShowWindow()
    {
        NetworkPrefabRegistryWindow window = GetWindow<NetworkPrefabRegistryWindow>("Prefab Registry");
        window.minSize = new Vector2(640, 480);
        window.LoadRegistry();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Network Prefab Registry", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Load", GUILayout.Width(80)))
            {
                LoadRegistry();
            }
        }

        EditorGUILayout.Space(8);
        _registry = (NetworkPrefabRegistry)EditorGUILayout.ObjectField("Registry", _registry, typeof(NetworkPrefabRegistry), false);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create Default Registry", GUILayout.Height(28)))
            {
                CreateDefaultRegistry();
            }

            GUI.enabled = _registry != null;
            if (GUILayout.Button("Refresh From Prefabs", GUILayout.Height(28)))
            {
                RefreshRegistry();
            }
            GUI.enabled = true;
        }

        EditorGUILayout.Space(8);
        DrawSummary();
        DrawEntries();
    }

    private void LoadRegistry()
    {
        _registry = AssetDatabase.LoadAssetAtPath<NetworkPrefabRegistry>(RegistryAssetPath);
        if (_registry == null)
        {
            _registry = Resources.Load<NetworkPrefabRegistry>(NetworkPrefabRegistry.ResourcesPath);
        }
    }

    private void CreateDefaultRegistry()
    {
        Directory.CreateDirectory("Assets/Resources");

        if (_registry == null)
        {
            _registry = AssetDatabase.LoadAssetAtPath<NetworkPrefabRegistry>(RegistryAssetPath);
        }

        if (_registry == null)
        {
            _registry = CreateInstance<NetworkPrefabRegistry>();
            AssetDatabase.CreateAsset(_registry, RegistryAssetPath);
        }

        RefreshRegistry();
        Selection.activeObject = _registry;
        EditorGUIUtility.PingObject(_registry);
    }

    private void RefreshRegistry()
    {
        if (_registry == null)
        {
            EditorUtility.DisplayDialog("Network Prefab Registry", "Create or assign a registry first.", "OK");
            return;
        }

        Undo.RecordObject(_registry, "Refresh Network Prefab Registry");

        Dictionary<GameObject, NetworkPrefabRegistry.Entry> byPrefab = new Dictionary<GameObject, NetworkPrefabRegistry.Entry>();
        foreach (NetworkPrefabRegistry.Entry entry in _registry.MutableEntries)
        {
            if (entry != null && entry.Prefab != null)
            {
                byPrefab[entry.Prefab] = entry;
            }
        }

        int addedCount = 0;
        string[] prefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { PrefabRoot });
        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                continue;
            }

            if (byPrefab.ContainsKey(prefab))
            {
                continue;
            }

            NetworkPrefabRegistry.Entry entry = new NetworkPrefabRegistry.Entry
            {
                Id = GenerateId(assetPath),
                Prefab = prefab
            };

            _registry.MutableEntries.Add(entry);
            byPrefab[prefab] = entry;
            addedCount++;
        }

        EditorUtility.SetDirty(_registry);
        AssetDatabase.SaveAssets();
        Repaint();
        EditorUtility.DisplayDialog("Network Prefab Registry", "Refresh complete. Added " + addedCount + " missing prefab(s).", "OK");
    }

    private void DrawSummary()
    {
        if (_registry == null)
        {
            EditorGUILayout.HelpBox("No registry loaded. Create the default registry at " + RegistryAssetPath + ".", MessageType.Warning);
            return;
        }

        int missingIdCount = 0;
        int missingPrefabCount = 0;
        int duplicateIdCount = 0;
        HashSet<string> seenIds = new HashSet<string>();

        foreach (NetworkPrefabRegistry.Entry entry in _registry.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Id))
            {
                missingIdCount++;
            }
            else if (!seenIds.Add(entry.Id))
            {
                duplicateIdCount++;
            }

            if (entry == null || entry.Prefab == null)
            {
                missingPrefabCount++;
            }
        }

        MessageType messageType = missingIdCount == 0 && missingPrefabCount == 0 && duplicateIdCount == 0 ? MessageType.Info : MessageType.Warning;
        EditorGUILayout.HelpBox(
            "Entries: " + _registry.Entries.Count +
            ", missing IDs: " + missingIdCount +
            ", missing prefabs: " + missingPrefabCount +
            ", duplicate IDs: " + duplicateIdCount + ".",
            messageType);
    }

    private void DrawEntries()
    {
        if (_registry == null)
        {
            return;
        }

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("ID", EditorStyles.boldLabel, GUILayout.Width(220));
            GUILayout.Label("Prefab", EditorStyles.boldLabel);
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (NetworkPrefabRegistry.Entry entry in _registry.Entries)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(entry.Id, GUILayout.Width(220));
                EditorGUILayout.ObjectField(entry.Prefab, typeof(GameObject), false);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private static string GenerateId(string assetPath)
    {
        const string prefix = "Assets/Resources/Prefabs/";
        string idSource = assetPath.StartsWith(prefix) ? assetPath.Substring(prefix.Length) : Path.GetFileName(assetPath);
        idSource = Path.Combine(Path.GetDirectoryName(idSource) ?? string.Empty, Path.GetFileNameWithoutExtension(idSource));
        return idSource.Replace("\\", "_").Replace("/", "_").Replace(" ", "_");
    }
}
