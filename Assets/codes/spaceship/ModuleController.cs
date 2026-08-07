using UnityEngine;
using System.Collections;
using Assets.codes.Network.SyncedIdentity;
using System;

namespace Assets.codes.spaceship
{
    [RequireComponent(typeof(Item))]
	public class ModuleController:MonoBehaviour
	{
        public ModuleSlot ConnectTo;
        private NetworkGameObject networkObject;
        private void Awake()
        {
            networkObject = GetComponent<NetworkGameObject>();
        }




        public void SetModuleData<T>(T data)
        {
            if (ConnectTo == null || ConnectTo.attachedModule == null)
            {
                Debug.LogWarning($"ModuleController {name} has no connected module.");
                return;
            }

            if (ConnectTo.attachedModule is Module<T> typedModule)
            {
                typedModule.SetData(data);
                return;
            }

            Debug.LogWarning($"Module {ConnectTo.attachedModule.name} does not accept data type {typeof(T).Name}.");

        }

        public void SetModuleDataInt(int data)
        {
            SetModuleData(data);
        }

        private void Start()
        {
            transform.parent = MainSpaceship.Instance.transform; 
            if (networkObject == null || networkObject.Identity == null)
            {
                Debug.LogWarning($"ModuleController {name} has no NetworkGameObject identity.");
                return;
            }

            string id = networkObject.Identity.Identifier;
            if(TryGetSlotIndexFromNetworkID(id, out int slotIndex))
            {
                ConnectTo = MainSpaceship.Instance.GetModuleSlot(slotIndex);
                if (ConnectTo != null)
                {
                    ConnectTo.moduleController = this;
                }
            }
            else
            {
                Debug.LogWarning($"Failed to extract slot index from NetworkID: {id}");
            }
        }
        private bool TryGetSlotIndexFromNetworkID(string networkID, out int slotIndex)
        {
            slotIndex = -1;

            if (string.IsNullOrEmpty(networkID))
                return false;

            const string prefix = "ModuleSlot_";
            if (!networkID.StartsWith(prefix))
                return false;

            int startIndex = prefix.Length;
            int endIndex = networkID.IndexOf('_', startIndex);
            string slotIndexText = endIndex >= 0
                ? networkID.Substring(startIndex, endIndex - startIndex)
                : networkID.Substring(startIndex);

            return int.TryParse(slotIndexText, out slotIndex);
        }
    }
}
