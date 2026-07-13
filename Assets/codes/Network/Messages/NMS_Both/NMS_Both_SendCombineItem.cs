using System;
using Assets.codes.items;
using Cysharp.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_SendCombineItem : NMS_BOTH_SERVERACTION
    {
        private readonly string lookingItemID;
        private readonly string holdingItemID;



        private readonly Item lookingItem;
        private readonly Item holdingItem;
        public NMS_Both_SendCombineItem(string lookingItemID, string holdingItemID) : base((int)packets.BothPackets.SendCombineItem)
        {
            this.lookingItemID = lookingItemID;
            this.holdingItemID = holdingItemID;
            lookingItem = NetworkSystem.Instance.GetComponentOfIdentity<Item>(lookingItemID);
            holdingItem = NetworkSystem.Instance.GetComponentOfIdentity<Item>(holdingItemID);
        }

        public static NMS_Both_SendCombineItem Read(Packet packet)
        {
            string lookingItemID = packet.ReadstringUNICODE();
            string holdingItemID = packet.ReadstringUNICODE();
            
            return new NMS_Both_SendCombineItem(lookingItemID, holdingItemID);
        }

        public override void Write(Packet packet)
        {
            packet.Write(lookingItemID);
            packet.Write(holdingItemID);
        }

        protected override void applyaction()
        {
            Debug.Log($"Combining {lookingItemID} and {holdingItemID}");

            if (lookingItem.HasItemType(ItemType.Processable) && holdingItem.HasItemType(ItemType.Processable))
            {
                if (lookingItem is CombinedProcessableItem c)
                {
                    c.CombineIntoThis(holdingItem);
                } else
                {

                }
            }
        }

        protected override void serverAction()
        {
            if(lookingItem.HasItemType(ItemType.Processable) && holdingItem.HasItemType(ItemType.Processable))
            {
                if (lookingItem is CombinedProcessableItem c)
                {
                    c.ServerAction_CombineIntoThis(holdingItem);
                } else
                {
                    NetworkSystem.Instance.CreateNewCombinedItem(lookingItem, holdingItem).Forget();
                }
            }
           
        }
    }
}
