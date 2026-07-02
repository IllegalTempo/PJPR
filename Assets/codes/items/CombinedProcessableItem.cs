using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Assets.codes.items
{
	public class CombinedProcessableItem: Item
	{
		public Dictionary<ItemDefinition,int> Processables = new Dictionary<ItemDefinition,int>();
		public void CombineIntoThis(Item item)
		{
			if(Processables.ContainsKey(item.AbstractItem))
			{
				Processables[item.AbstractItem] += 1;
			} else
			{
				Processables[item.AbstractItem] = 1;
			}
		}
		public void ServerAction_Combine(Item item)
		{
			GameCore.Instance.DestroyNetworkItem(item);
		}
	}
}