using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkObjectIdWindow : EditorWindow
{
    private readonly List<NetworkObjectInfo> _objects = new List<NetworkObjectInfo>();
    private Vector2 _scroll;
    private bool _fixDuplicateIds;
    private bool _replaceRuntimeIds;

    [MenuItem("Tools/Network Objects/ID Tools")]
    public static void ShowWindow()
    {
        NetworkObjectIdWindow window = GetWindow<NetworkObjectIdWindow>("NetworkObject IDs");
        window.minSize = new Vector2(720, 480);
        window.ScanOpenScenes();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("NetworkObject IDs", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Scan Open Scenes", GUILayout.Width(140)))
            {
                ScanOpenScenes();
            }
        }

        EditorGUILayout.Space(8);
        DrawSummary();

        EditorGUILayout.Space(8);
        _fixDuplicateIds = EditorGUILayout.ToggleLeft("Regenerate duplicate IDs after the first object that uses them", _fixDuplicateIds);
        _replaceRuntimeIds = EditorGUILayout.ToggleLeft("Replace runtime-style scene IDs like nouid_1", _replaceRuntimeIds);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Fill Missing IDs", GUILayout.Height(28)))
            {
                FillIds(false);
            }

            if (GUILayout.Button("Fill Missing / Fix Checked", GUILayout.Height(28)))
            {
                FillIds(true);
            }
        }

        EditorGUILayout.Space(10);
        DrawObjectTable();
    }

    private void DrawSummary()
    {
        int missingCount = 0;
        int duplicateCount = 0;
        int runtimeStyleCount = 0;

        foreach (NetworkObjectInfo info in _objects)
        {
            if (info.Status == IdStatus.Missing)
            {
                missingCount++;
            }
            else if (info.Status == IdStatus.Duplicate)
            {
                duplicateCount++;
            }
            else if (info.Status == IdStatus.RuntimeStyle)
            {
                runtimeStyleCount++;
            }
        }

        MessageType messageType = missingCount == 0 && duplicateCount == 0 && runtimeStyleCount == 0 ? MessageType.Info : MessageType.Warning;
        EditorGUILayout.HelpBox(
            "Found " + _objects.Count + " NetworkObject(s). Missing: " + missingCount +
            ", duplicates: " + duplicateCount +
            ", runtime-style scene IDs: " + runtimeStyleCount + ".",
            messageType);
    }

    private void DrawObjectTable()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Status", EditorStyles.boldLabel, GUILayout.Width(110));
            GUILayout.Label("Object", EditorStyles.boldLabel, GUILayout.Width(180));
            GUILayout.Label("Scene", EditorStyles.boldLabel, GUILayout.Width(140));
            GUILayout.Label("Identifier", EditorStyles.boldLabel);
            GUILayout.Label("", GUILayout.Width(70));
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (NetworkObjectInfo info in _objects)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(GetStatusText(info.Status), GUILayout.Width(110));
                GUILayout.Label(info.Object.name, GUILayout.Width(180));
                GUILayout.Label(info.SceneName, GUILayout.Width(140));
                EditorGUILayout.SelectableLabel(info.Identifier, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                if (GUILayout.Button("Select", GUILayout.Width(70)))
                {
                    Selection.activeObject = info.Object;
                    EditorGUIUtility.PingObject(info.Object);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void ScanOpenScenes()
    {
        _objects.Clear();
        NetworkPrefab[] networkObjects = Resources.FindObjectsOfTypeAll<NetworkPrefab>();
        Dictionary<string, NetworkObjectInfo> firstById = new Dictionary<string, NetworkObjectInfo>();

        foreach (NetworkPrefab networkObject in networkObjects)
        {
            if (networkObject == null || EditorUtility.IsPersistent(networkObject))
            {
                continue;
            }

            GameObject gameObject = networkObject.gameObject;
            Scene scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            string id = networkObject.Identifier ?? "";
            IdStatus status = GetBaseStatus(id);
            NetworkObjectInfo info = new NetworkObjectInfo(networkObject, scene.name, id, status);

            if (!string.IsNullOrWhiteSpace(id))
            {
                if (firstById.ContainsKey(id))
                {
                    info.Status = IdStatus.Duplicate;
                    if (firstById[id].Status == IdStatus.Ok)
                    {
                        firstById[id].Status = IdStatus.Duplicate;
                    }
                }
                else
                {
                    firstById.Add(id, info);
                }
            }

            _objects.Add(info);
        }

        _objects.Sort((left, right) =>
        {
            int sceneCompare = string.CompareOrdinal(left.SceneName, right.SceneName);
            return sceneCompare != 0 ? sceneCompare : string.CompareOrdinal(left.Object.name, right.Object.name);
        });

        Repaint();
    }

    private void FillIds(bool applyCheckedFixes)
    {
        if (_objects.Count == 0)
        {
            ScanOpenScenes();
        }

        HashSet<string> usedIds = new HashSet<string>();
        int changedCount = 0;

        foreach (NetworkObjectInfo info in _objects)
        {
            NetworkPrefab networkObject = info.Object;
            if (networkObject == null)
            {
                continue;
            }

            string id = networkObject.Identifier ?? "";
            bool shouldGenerate =
                string.IsNullOrWhiteSpace(id) ||
                (applyCheckedFixes && _replaceRuntimeIds && IsRuntimeStyleId(id)) ||
                (applyCheckedFixes && _fixDuplicateIds && usedIds.Contains(id));

            if (shouldGenerate)
            {
                Undo.RecordObject(networkObject, "Generate NetworkObject Identifier");
                networkObject.Identifier = GenerateId(networkObject);
                EditorUtility.SetDirty(networkObject);
                EditorSceneManager.MarkSceneDirty(networkObject.gameObject.scene);
                changedCount++;
            }

            usedIds.Add(networkObject.Identifier);
        }

        ScanOpenScenes();
        EditorUtility.DisplayDialog("NetworkObject IDs", "Updated " + changedCount + " NetworkObject ID(s).", "OK");
    }

    private static IdStatus GetBaseStatus(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return IdStatus.Missing;
        }

        if (IsRuntimeStyleId(id))
        {
            return IdStatus.RuntimeStyle;
        }

        return IdStatus.Ok;
    }

    private static bool IsRuntimeStyleId(string id)
    {
        return id.StartsWith("nouid_", StringComparison.Ordinal);
    }

    private static string GenerateId(NetworkPrefab networkObject)
    {
        string sceneName = string.IsNullOrWhiteSpace(networkObject.gameObject.scene.name)
            ? "unsaved_scene"
            : Sanitize(networkObject.gameObject.scene.name);

        return "scene:" + sceneName + ":" + Guid.NewGuid().ToString("N");
    }

    private static string Sanitize(string value)
    {
        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    private static string GetStatusText(IdStatus status)
    {
        switch (status)
        {
            case IdStatus.Missing:
                return "Missing";
            case IdStatus.Duplicate:
                return "Duplicate";
            case IdStatus.RuntimeStyle:
                return "Runtime style";
            default:
                return "OK";
        }
    }

    private enum IdStatus
    {
        Ok,
        Missing,
        Duplicate,
        RuntimeStyle
    }

    private sealed class NetworkObjectInfo
    {
        public readonly NetworkPrefab Object;
        public readonly string SceneName;
        public readonly string Identifier;
        public IdStatus Status;

        public NetworkObjectInfo(NetworkPrefab obj, string sceneName, string identifier, IdStatus status)
        {
            Object = obj;
            SceneName = sceneName;
            Identifier = identifier;
            Status = status;
        }
    }
}
