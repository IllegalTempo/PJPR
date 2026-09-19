using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissionTravelSceneSetup
{
    private const string EscapePrefabPath = "Assets/Prefabs/Mission/Escape The Blackhole/EscapeBlackholeMission.prefab";
    private const string PeakPrefabPath = "Assets/Prefabs/Mission/Peak of Energy/Peak Of Energy.prefab";
    private const string PortalPrefabPath = "Assets/Prefabs/Portal.prefab";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string MissionSceneFolder = "Assets/Scenes/Missions";

    private static readonly Dictionary<string, string> SceneNamesByMission = new()
    {
        { "Escape the Blackhole", "EscapeTheBlackhole" },
        { "Peak of Energy", "PeakOfEnergy" },
        { "Mission3", "Mission3" },
        { "Mission4", "Mission4" },
        { "Mission5", "Mission5" },
    };

    [MenuItem("Tools/Missions/Generate Additive Mission Scenes")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets/Scenes", "Missions");
        EnsurePortalPrefab();
        EnsureMissionScene("EscapeTheBlackhole", EscapePrefabPath);
        EnsureMissionScene("PeakOfEnergy", PeakPrefabPath);
        EnsureMissionScene("Mission3", null);
        EnsureMissionScene("Mission4", null);
        EnsureMissionScene("Mission5", null);
        UpdateMissionDataAssets();
        UpdateBuildSettings();
        ConfigureMainPauseRoots();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[MissionTravelSceneSetup] Mission travel assets generated successfully.");
    }

    private static void EnsurePortalPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PortalPrefabPath);
        try
        {
            NetworkPrefabIdentity prefabIdentity = root.GetComponent<NetworkPrefabIdentity>();
            if (prefabIdentity == null)
                prefabIdentity = root.AddComponent<NetworkPrefabIdentity>();

            foreach (NetworkIdentity identity in root.GetComponents<NetworkIdentity>())
            {
                if (identity != prefabIdentity)
                    UnityEngine.Object.DestroyImmediate(identity);
            }

            Assets.codes.Network.SyncedIdentity.NetworkGameObject networkObject =
                root.GetComponent<Assets.codes.Network.SyncedIdentity.NetworkGameObject>();
            if (networkObject == null)
                networkObject = root.AddComponent<Assets.codes.Network.SyncedIdentity.NetworkGameObject>();
            networkObject.Identity = prefabIdentity;
            if (root.GetComponent<MissionPortal>() == null)
                root.AddComponent<MissionPortal>();

            SphereCollider trigger = root.GetComponent<SphereCollider>();
            if (trigger == null)
                trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            if (trigger.radius <= 0f)
                trigger.radius = 5f;

            PrefabUtility.SaveAsPrefabAsset(root, PortalPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureMissionScene(string sceneName, string gameplayPrefabPath)
    {
        string scenePath = $"{MissionSceneFolder}/{sceneName}.unity";
        Scene scene = System.IO.File.Exists(scenePath)
            ? EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        MissionSpawnPoint[] spawnPoints = GetSceneComponents<MissionSpawnPoint>(scene);
        MissionSpawnPoint spawnPoint;
        if (spawnPoints.Length == 0)
        {
            spawnPoint = new GameObject("MissionSpawnPoint").AddComponent<MissionSpawnPoint>();
            SceneManager.MoveGameObjectToScene(spawnPoint.gameObject, scene);
        }
        else
        {
            spawnPoint = spawnPoints[0];
            for (int i = 1; i < spawnPoints.Length; i++)
                UnityEngine.Object.DestroyImmediate(spawnPoints[i].gameObject);
        }

        spawnPoint.gameObject.name = "MissionSpawnPoint";
        if (!string.IsNullOrEmpty(gameplayPrefabPath))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(gameplayPrefabPath);
            if (prefab == null)
                throw new InvalidOperationException($"Missing mission prefab: {gameplayPrefabPath}");

            Type wrapperType = sceneName == "EscapeTheBlackhole"
                ? typeof(EscapeBlackholeMission)
                : typeof(PeakOfEnergyMission);

            List<GameObject> instances = scene.GetRootGameObjects()
                .Where(root => PrefabUtility.GetCorrespondingObjectFromSource(root) == prefab)
                .ToList();
            if (instances.Count == 0)
                instances.Add((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene));
            for (int i = 1; i < instances.Count; i++)
                UnityEngine.Object.DestroyImmediate(instances[i]);

            if (GetSceneComponents(scene, wrapperType).Length == 0)
            {
                GameObject wrapperObject = new GameObject($"{sceneName}Mission");
                SceneManager.MoveGameObjectToScene(wrapperObject, scene);
                wrapperObject.AddComponent(wrapperType);
            }
        }

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void UpdateMissionDataAssets()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:MissionData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            MissionData data = AssetDatabase.LoadAssetAtPath<MissionData>(path);
            if (data == null || !SceneNamesByMission.TryGetValue(data.missionName, out string sceneName))
            {
                if (data != null)
                    Debug.LogWarning($"[MissionTravelSceneSetup] Skipping unknown mission '{data.missionName}' at {path}.");
                continue;
            }

            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("missionSceneName").stringValue = sceneName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }
    }

    private static void UpdateBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        foreach (string sceneName in SceneNamesByMission.Values.Distinct())
        {
            string path = $"{MissionSceneFolder}/{sceneName}.unity";
            int index = scenes.FindIndex(entry => entry.path == path);
            if (index >= 0)
                scenes[index] = new EditorBuildSettingsScene(path, true);
            else
                scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ConfigureMainPauseRoots()
    {
        Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        string[] names = { "Docks", "Directional Light", "Global Volume" };
        GameObject[] sceneRoots = scene.GetRootGameObjects();
        GameObject[] selected = names.Select(name =>
        {
            GameObject[] matches = sceneRoots.Where(root => root.name == name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Main scene must contain exactly one root named '{name}', found {matches.Length}.");
            return matches[0];
        }).ToArray();

        MissionManager[] managers = GetSceneComponents<MissionManager>(scene);
        if (managers.Length != 1)
            throw new InvalidOperationException($"Main scene must contain exactly one MissionManager, found {managers.Length}.");

        SerializedObject serialized = new SerializedObject(managers[0]);
        SerializedProperty roots = serialized.FindProperty("mainScenePauseRoots");
        if (roots == null)
            throw new InvalidOperationException("MissionManager.mainScenePauseRoots was not found.");
        roots.arraySize = selected.Length;
        for (int i = 0; i < selected.Length; i++)
            roots.GetArrayElementAtIndex(i).objectReferenceValue = selected[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[MissionTravelSceneSetup] Main pause roots: {string.Join(", ", names)}");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainScenePath);
    }

    private static T[] GetSceneComponents<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    }

    private static Component[] GetSceneComponents(Scene scene, Type componentType)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren(componentType, true))
            .ToArray();
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }
}
