using UnityEngine;
using Cysharp.Threading.Tasks;
using Assets.codes.Network.SyncedIdentity;

/// <summary>
/// Spawns the registered Hammer networked item into the world at mission start so players
/// can pick it up like any other networked item (rather than hand-placing a loose scene
/// hammer which fights the networked-item architecture).
///
/// Server/offline only: NetworkSystem.CreateNetworkObject is a no-op on client mirrors.
/// The Hammer must have a valid registry entry ("Hammer" -> a PrefabDefinition whose
/// Item Prefab has NetworkGameObject + NetworkPrefabIdentity + HammerItem + rigidbody/collider).
/// </summary>
public class PeakOfEnergyHammerSpawner : MonoBehaviour
{
    [Header("Hammer Spawn")]
    [Tooltip("Prefab ID of the Hammer in the NetworkPrefabRegistry. Defaults to 'Hammer'.")]
    [SerializeField] private string hammerPrefabId = "Hammer";
    [Tooltip("Where to spawn the hammer. Leave empty to spawn near the MainSpaceship.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Fallback offset from the ship/ring used when no spawn point is set.")]
    [SerializeField] private Vector3 fallbackOffset = new Vector3(2f, 1f, 2f);
    [Tooltip("Should the hammer spawn automatically at Start?")]
    [SerializeField] private bool spawnOnStart = true;

    private void Start()
    {
        if (!NetworkSystem.Instance.IsWorldManager)
        {
            enabled = false;
            return;
        }

        if (spawnOnStart)
            SpawnHammer().Forget();
    }

    [ContextMenu("Spawn Hammer")]
    public async UniTask SpawnHammer()
    {
        if (string.IsNullOrEmpty(hammerPrefabId))
        {
            Debug.LogWarning("[PeakOfEnergyHammerSpawner] No Hammer prefab ID set.");
            return;
        }

        Vector3 position = ResolveSpawnPosition();

        NetworkGameObject nobj = await NetworkSystem.Instance.CreateNetworkObject(hammerPrefabId, position, Quaternion.identity, 0);
        if (nobj == null)
        {
            Debug.LogWarning($"[PeakOfEnergyHammerSpawner] Failed to create '{hammerPrefabId}'. Is it registered in NetworkPrefabRegistry with a completed PrefabDefinition (Item Prefab set)?");
            return;
        }

        Debug.Log($"[PeakOfEnergyHammerSpawner] Spawned '{hammerPrefabId}' at {position}.");
    }

    private Vector3 ResolveSpawnPosition()
    {
        if (spawnPoint != null)
            return spawnPoint.position;

        if (MainSpaceship.Instance != null)
            return MainSpaceship.Instance.transform.position + fallbackOffset;

        if (PeakOfEnergyManager.Instance != null)
            return PeakOfEnergyManager.Instance.transform.position + fallbackOffset;

        return transform.position + fallbackOffset;
    }
}
