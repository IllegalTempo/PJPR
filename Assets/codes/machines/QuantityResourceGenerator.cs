using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.codes.machines
{
	public class QuantityResourceGenerator: SyncedMachine
	{
		[SerializeField] private PrefabDefinition resource;
		Vector3 spawnpos;
        protected override void Start()
        {
			base.Start();
			spawnpos = transform.position;
            Transform spawnSpot = transform.Find("SpawnSpot");

            if (spawnSpot != null)
            {
                spawnpos = spawnSpot.position;
            }
        }
        protected override async void ServerActionOnInteract_press(PlayerMain who)
		{
            string resourcePrefabId = NetworkSystem.Instance.GetPrefabId(resource);
            Debug.Log($"[WaterGen] ServerActionOnInteract — spawning {resourcePrefabId}");

            if (!string.IsNullOrWhiteSpace(resourcePrefabId))
			{
                await UniTask.Delay(100);
                NetworkSystem.Instance.CreateNetworkObject(resourcePrefabId,spawnpos,Quaternion.identity,0).Forget();
			}
		}
		
		public override void OnInteract_press(PlayerMain who)
		{
            Debug.Log($"[WaterGen] OnInteract — IsServer={NetworkSystem.Instance?.IsServer}, identity={identity?.Identifier}");

            string resourcePrefabId = NetworkSystem.Instance.GetPrefabId(resource);
            if (string.IsNullOrWhiteSpace(resourcePrefabId))
			{
				Debug.LogWarning($"{name} has no resource prefab ID.");
				return;
			}

			
            
            base.OnInteract_press(who);
            
        }

    }
}
