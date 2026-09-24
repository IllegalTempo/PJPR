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

        public void Initialize(ModuleSlot slot)
        {
            ConnectTo = slot;
            if (ConnectTo != null)
            {
                ConnectTo.moduleController = this;
            }
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

        public void SetModuleDataBool(bool data)
        {
            SetModuleData(data);
        }

        private void Start()
        {
            if (ConnectTo != null)
            {
                ConnectTo.moduleController = this;
                return;
            }

            if (networkObject == null || networkObject.Identity == null)
            {
                Debug.LogWarning($"ModuleController {name} has no NetworkGameObject identity.");
                return;
            }

            string id = networkObject.Identity.Identifier;
            if(ModuleControllerSlotLink.TryResolve(id, out ModuleSlot slot))
            {
                Initialize(slot);
            }
            else
            {
                Debug.LogWarning($"Failed to resolve module slot from NetworkID: {id}");
            }
        }
    }
}
