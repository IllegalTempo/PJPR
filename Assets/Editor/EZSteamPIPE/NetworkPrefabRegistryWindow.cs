using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NetworkPrefabRegistryWindow : EditorWindow
{
    private const string PrefabRoot = "Assets/Resources/Prefabs";
    private const string RegistryPath = PrefabRoot + "/NetworkPrefabRegistry.asset";

    private NetworkPrefabRegistry _registry;
    private SerializedObject _serializedRegistry;

    [MenuItem("Tools/Network Objects/Prefab Lookup")]
    public static void ShowWindow()
    {
        NetworkPrefabRegistryWindow window = GetWindow<NetworkPrefabRegistryWindow>("Prefab Lookup");
        window.minSize = new Vector2(720, 480);
        window.LoadRegistry();
    }

    private void OnEnable()
    {
        LoadRegistry();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Network Prefab Lookup", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create/Load", GUILayout.Width(90)))
            {
                LoadOrCreateRegistry();
            }
        }

        EditorGUILayout.HelpBox("Runtime network prefab lookup is loaded from " + RegistryPath + ". This registry is the source of truth for prefab IDs.", MessageType.Info);

        if (_registry == null)
        {
            EditorGUILayout.HelpBox("No NetworkPrefabRegistry asset found.", MessageType.Warning);
            if (GUILayout.Button("Create Registry", GUILayout.Height(28)))
            {
                LoadOrCreateRegistry();
            }

            return;
        }

        DrawSummary();
        DrawEntries();
    }

    private void LoadRegistry()
    {
        _registry = AssetDatabase.LoadAssetAtPath<NetworkPrefabRegistry>(RegistryPath);
        _serializedRegistry = _registry != null ? new SerializedObject(_registry) : null;
        Repaint();
    }

    private void LoadOrCreateRegistry()
    {
        LoadRegistry();
        if (_registry != null)
        {
            return;
        }

        EnsureFolder(PrefabRoot);
        _registry = ScriptableObject.CreateInstance<NetworkPrefabRegistry>();
        AssetDatabase.CreateAsset(_registry, RegistryPath);
        AssetDatabase.SaveAssets();
        _serializedRegistry = new SerializedObject(_registry);
        Selection.activeObject = _registry;
        EditorGUIUtility.PingObject(_registry);
        Repaint();
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private void DrawSummary()
    {
        int missingIdCount = 0;
        int missingDefinitionCount = 0;
        int missingPrefabCount = 0;
        int duplicateIdCount = 0;
        HashSet<string> seenIds = new HashSet<string>();

        foreach (NetworkPrefabRegistry.Entry entry in _registry.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.PrefabId))
            {
                missingIdCount++;
            }
            else if (!seenIds.Add(entry.PrefabId))
            {
                duplicateIdCount++;
            }

            if (entry == null || entry.PrefabDefinition == null)
            {
                missingDefinitionCount++;
            }
            else if (entry.PrefabDefinition.itemPrefab == null)
            {
                missingPrefabCount++;
            }
        }

        MessageType messageType = missingIdCount == 0 && missingDefinitionCount == 0 && missingPrefabCount == 0 && duplicateIdCount == 0 ? MessageType.Info : MessageType.Warning;
        EditorGUILayout.HelpBox(
            "Registry entries: " + _registry.Entries.Count +
            ", missing IDs: " + missingIdCount +
            ", missing definitions: " + missingDefinitionCount +
            ", missing prefabs: " + missingPrefabCount +
            ", duplicate IDs: " + duplicateIdCount + ".",
            messageType);
    }

    private void DrawEntries()
    {
        _serializedRegistry.Update();
        SerializedProperty entries = _serializedRegistry.FindProperty("entries");
        EditorGUILayout.PropertyField(entries, true);
        _serializedRegistry.ApplyModifiedProperties();
    }
}
