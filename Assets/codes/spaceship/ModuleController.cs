using UnityEngine;
using System.Collections;
using Assets.codes.Network.SyncedIdentity;

namespace Assets.codes.spaceship
{
	public class ModuleController:MonoBehaviour
	{
        public ModuleSlot ConnectTo;
        private NetworkGameObject networkObject;
        private void Awake()
        {
            networkObject = GetComponent<NetworkGameObject>();
        }

        private void Start()
        {
            if (networkObject == null || networkObject.Identity == null)
            {
                Debug.LogWarning($"ModuleController {name} has no NetworkGameObject identity.");
                return;
            }

            string id = networkObject.Identity.Identifier;
            if(TryGetSlotIndexFromUid(id, out int slotIndex))
            {
                ConnectTo = MainSpaceship.Instance.GetModuleSlot(slotIndex);
                if (ConnectTo != null)
                {
                    ConnectTo.moduleController = this;
                }
            }
            else
            {
                Debug.LogWarning($"Failed to extract slot index from UID: {id}");
            }
        }
        private bool TryGetSlotIndexFromUid(string uid, out int slotIndex)
        {
            slotIndex = -1;

            if (string.IsNullOrEmpty(uid))
                return false;

            const string prefix = "ModuleSlot_";
            if (!uid.StartsWith(prefix))
                return false;

            int startIndex = prefix.Length;
            int endIndex = uid.IndexOf('_', startIndex);
            string slotIndexText = endIndex >= 0
                ? uid.Substring(startIndex, endIndex - startIndex)
                : uid.Substring(startIndex);

            return int.TryParse(slotIndexText, out slotIndex);
        }
    }
}
