using Assets.codes.Network.Messages;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.codes.items
{
	public class CombinedProcessableItem : Item
	{
		public Dictionary<PrefabDefinition, int> Processables = new Dictionary<PrefabDefinition, int>();
		public void CombineIntoThis(Item item) //run by both client and server
		{
            PrefabDefinition def = item.GetNetworkObject().AbstractObject;
            if (Processables.ContainsKey(def))
			{
				Processables[def] += 1;
			} else
			{
				Processables[def] = 1;
			}
            GameCore.Instance.DestroyNetworkObject(item.GetNetworkObject().Identity.Identifier);

        }
        public void ServerAction_CombineIntoThis(Item item)
		{
			//GameCore.Instance.ServerDestroyNetworkItem(item);

		}
	}
}