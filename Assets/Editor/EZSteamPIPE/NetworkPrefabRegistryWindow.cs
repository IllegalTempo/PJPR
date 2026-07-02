using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NetworkPrefabRegistryWindow : EditorWindow
{
    private const string PrefabRoot = "Assets/Resources/Prefabs";

    private readonly List<ItemDefinition> _itemDefinitions = new List<ItemDefinition>();
    private Vector2 _scroll;

    [MenuItem("Tools/Network Objects/Prefab Lookup")]
    public static void ShowWindow()
    {
        NetworkPrefabRegistryWindow window = GetWindow<NetworkPrefabRegistryWindow>("Prefab Lookup");
        window.minSize = new Vector2(720, 480);
        window.RefreshDefinitions();
    }

    private void OnEnable()
    {
        RefreshDefinitions();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("Network Prefab Lookup", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", GUILayout.Width(90)))
            {
                RefreshDefinitions();
            }
        }

        EditorGUILayout.HelpBox("Runtime network prefab lookup is generated from ItemDefinition assets under " + PrefabRoot + ". The ItemDefinition prefabID is the source of truth.", MessageType.Info);

        DrawSummary();
        DrawEntries();
    }

    private void RefreshDefinitions()
    {
        _itemDefinitions.Clear();

        if (!AssetDatabase.IsValidFolder(PrefabRoot))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { PrefabRoot });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            if (itemDefinition != null)
            {
                _itemDefinitions.Add(itemDefinition);
            }
        }

        _itemDefinitions.Sort((left, right) => string.CompareOrdinal(left.prefabID, right.prefabID));
        Repaint();
    }

    private void DrawSummary()
    {
        int missingIdCount = 0;
        int missingPrefabCount = 0;
        int duplicateIdCount = 0;
        HashSet<string> seenIds = new HashSet<string>();

        foreach (ItemDefinition itemDefinition in _itemDefinitions)
        {
            if (string.IsNullOrWhiteSpace(itemDefinition.prefabID))
            {
                missingIdCount++;
            }
            else if (!seenIds.Add(itemDefinition.prefabID))
            {
                duplicateIdCount++;
            }

            if (itemDefinition.itemPrefab == null)
            {
                missingPrefabCount++;
            }
        }

        MessageType messageType = missingIdCount == 0 && missingPrefabCount == 0 && duplicateIdCount == 0 ? MessageType.Info : MessageType.Warning;
        EditorGUILayout.HelpBox(
            "Item definitions: " + _itemDefinitions.Count +
            ", missing IDs: " + missingIdCount +
            ", missing prefabs: " + missingPrefabCount +
            ", duplicate IDs: " + duplicateIdCount + ".",
            messageType);
    }

    private void DrawEntries()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Prefab ID", EditorStyles.boldLabel, GUILayout.Width(220));
            GUILayout.Label("Item Definition", EditorStyles.boldLabel, GUILayout.Width(220));
            GUILayout.Label("Prefab", EditorStyles.boldLabel);
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (ItemDefinition itemDefinition in _itemDefinitions)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(itemDefinition.prefabID, GUILayout.Width(220), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                EditorGUILayout.ObjectField(itemDefinition, typeof(ItemDefinition), false, GUILayout.Width(220));
                EditorGUILayout.ObjectField(itemDefinition.itemPrefab, typeof(GameObject), false);
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
