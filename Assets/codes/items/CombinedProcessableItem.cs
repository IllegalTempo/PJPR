using Assets.codes.Network.Messages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.codes.items
{
	public class CombinedProcessableItem : Item
	{
		public Dictionary<ItemDefinition, int> Processables = new Dictionary<ItemDefinition, int>();
		public void CombineIntoThis(Item item) //run by both client and server
		{
			if (Processables.ContainsKey(item.AbstractItem))
			{
				Processables[item.AbstractItem] += 1;
			} else
			{
				Processables[item.AbstractItem] = 1;
			}
            GameCore.Instance.DestroyNetworkIdentity(item.GetNetworkObject().Identity.Identifier);

        }
        public void ServerAction_CombineIntoThis(Item item)
		{
			GameCore.Instance.ServerDestroyNetworkItem(item);

		}
	}
}