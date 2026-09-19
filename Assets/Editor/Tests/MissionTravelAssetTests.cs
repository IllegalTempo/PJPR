using System.Collections.Generic;
using System.Linq;
using Assets.codes.Network.SyncedIdentity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionTravelAssetTests
{
    private const string SceneFolder = "Assets/Scenes/Missions";

    [Test]
    public void GeneratedScenes_HaveOneSpawnPointAndExpectedMissionWrapper()
    {
        AssertScene("EscapeTheBlackhole", typeof(EscapeBlackholeMission));
        AssertScene("PeakOfEnergy", typeof(PeakOfEnergyMission));
        AssertScene("Mission3", null);
        AssertScene("Mission4", null);
        AssertScene("Mission5", null);
    }

    [Test]
    public void Portal_IsNetworkedTriggerAndRegistered()
    {
        GameObject portal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Portal.prefab");
        Assert.That(portal, Is.Not.Null);
        Assert.That(portal.GetComponent<NetworkPrefabIdentity>(), Is.Not.Null);
        Assert.That(portal.GetComponent<NetworkGameObject>(), Is.Not.Null);
        Assert.That(portal.GetComponent<MissionPortal>(), Is.Not.Null);
        Collider trigger = portal.GetComponent<Collider>();
        Assert.That(trigger, Is.Not.Null);
        Assert.That(trigger.isTrigger, Is.True);

        NetworkPrefabRegistry registry = AssetDatabase.LoadAssetAtPath<NetworkPrefabRegistry>(
            "Assets/Prefabs/NetworkPrefabRegistry.asset");
        Assert.That(registry.TryGetDefinition("Mission_Portal", out PrefabDefinition definition), Is.True);
        Assert.That(definition, Is.Not.Null);
    }

    [Test]
    public void MissionScenes_AreEnabledAndMissionDataIsMapped()
    {
        var enabled = new HashSet<string>(EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path));
        string[] names = { "EscapeTheBlackhole", "PeakOfEnergy", "Mission3", "Mission4", "Mission5" };
        foreach (string name in names)
            Assert.That(enabled, Does.Contain($"{SceneFolder}/{name}.unity"));

        var expected = new Dictionary<string, string>
        {
            { "Escape the Blackhole", "EscapeTheBlackhole" },
            { "Peak of Energy", "PeakOfEnergy" },
            { "Mission3", "Mission3" },
            { "Mission4", "Mission4" },
            { "Mission5", "Mission5" },
        };
        foreach (string guid in AssetDatabase.FindAssets("t:MissionData"))
        {
            MissionData data = AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data != null && expected.TryGetValue(data.missionName, out string sceneName))
                Assert.That(data.missionSceneName, Is.EqualTo(sceneName));
        }
    }

    private static void AssertScene(string sceneName, System.Type expectedWrapper)
    {
        Scene scene = EditorSceneManager.OpenScene($"{SceneFolder}/{sceneName}.unity", OpenSceneMode.Additive);
        try
        {
            Component[] spawnPoints = GetComponents(scene, typeof(MissionSpawnPoint));
            Assert.That(spawnPoints, Has.Length.EqualTo(1), sceneName);

            int wrapperCount = GetComponents(scene, typeof(EscapeBlackholeMission)).Length +
                               GetComponents(scene, typeof(PeakOfEnergyMission)).Length;
            if (expectedWrapper == null)
                Assert.That(wrapperCount, Is.Zero, sceneName);
            else
                Assert.That(GetComponents(scene, expectedWrapper), Has.Length.EqualTo(1), sceneName);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static Component[] GetComponents(Scene scene, System.Type type)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren(type, true))
            .ToArray();
    }
}
