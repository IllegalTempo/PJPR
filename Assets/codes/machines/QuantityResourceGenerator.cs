using UnityEngine;
using System.Collections;
using Assets.codes.Network.Messages;
using JetBrains.Annotations;
using Cysharp.Threading.Tasks;

namespace Assets.codes.machines
{
	public class QuantityResourceGenerator: Machine
	{
		[SerializeField] private ItemDefinition resource;
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
        public override async void ServerActionOnInteract()
		{
            Debug.Log($"[WaterGen] ServerActionOnInteract — spawning {resource?.prefabID}");

            if (resource != null)
			{
                await UniTask.Delay(100);
                NetworkSystem.Instance.CreateNetworkObject(resource.prefabID,spawnpos,Quaternion.identity,0).Forget();
			}
		}
		
		public override void OnInteract_press(PlayerMain who)
		{
            Debug.Log($"[WaterGen] OnInteract — IsServer={NetworkSystem.Instance?.IsServer}, identity={identity?.Identifier}");

            if (resource == null || string.IsNullOrWhiteSpace(resource.prefabID))
			{
				Debug.LogWarning($"{name} has no resource prefab ID.");
				return;
			}

			
            
			string ResourcePrefabID = resource.prefabID;
            base.OnInteract_press(who);
            
        }

        public override void ShareActionOnInteract()
        {
        }
    }
}
