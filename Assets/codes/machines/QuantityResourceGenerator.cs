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
			if (resource != null)
			{
				await UniTask.Delay(100);
                NetworkSystem.Instance.CreateNetworkObject(resource.prefabID,spawnpos,Quaternion.identity,0).Forget();
			}
		}
		
		public override void OnInteract(PlayerMain who)
		{
			if (resource == null || string.IsNullOrWhiteSpace(resource.prefabID))
			{
				Debug.LogWarning($"{name} has no resource prefab ID.");
				return;
			}

			

			string ResourcePrefabID = resource.prefabID;
            NMS_Both_MachineInteract msg = new NMS_Both_MachineInteract(identity.Identifier);
			msg.SendMessageAsServerOrClient();
        }

        public override void ShareActionOnInteract()
        {
        }
    }
}
