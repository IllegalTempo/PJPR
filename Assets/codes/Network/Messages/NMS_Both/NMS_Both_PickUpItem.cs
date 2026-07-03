using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_PickUpItem : NMS_BOTH_SHARE
    {
        private readonly string itemId;
        private readonly ulong pickedUpBy;

        public NMS_Both_PickUpItem(string itemId, ulong pickedUpBy) : base((int)packets.BothPackets.PickUpItem)
        {
            this.itemId = itemId;
            this.pickedUpBy = pickedUpBy;
        }

        public static NMS_Both_PickUpItem Read(Packet packet)
        {
            return new NMS_Both_PickUpItem(packet.ReadstringUNICODE(), packet.Readulong());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(itemId);
            packet.Write(pickedUpBy);
        }

        protected override void applyaction()
        {
            if(NetworkSystem.Instance.FindNetworkIdentity.ContainsKey(itemId))
            {
                NetworkSystem.Instance.GetComponentOfIdentity<Item>(itemId).Network_onPickUPorDrop(pickedUpBy);

            } else
            {
                throw new NO_Not_Found(itemId);
            }

        }
        public override void ServerHandle(NetworkPlayer player)
        {
            if (!NetworkSystem.Instance.FindNetworkIdentity.TryGetValue(itemId, out NetworkIdentity networkObject))
            {
                throw new NO_Not_Found(itemId);
            }

            bool isDropRequest = pickedUpBy == 0;
            if (isDropRequest)
            {
                if (((NetworkPrefabIdentity)networkObject).Sovereignty != player.steamId)
                {
                    Debug.LogWarning($"Rejected drop for {itemId}: {player.steamId} does not own it.");
                    return;
                }
            }
            else if (player.steamId != pickedUpBy)
            {
                if (NetworkSystem.Instance.FindNetworkIdentity[itemId].Sovereignty != 0)
                {
                    Debug.LogWarning("Item pick up by two person at the same time");
                    return;
                }
                    Debug.LogWarning($"Rejected pickup for {itemId}: sender {player.steamId} tried to set owner {pickedUpBy}.");
                return;
                
                
            }

            base.ServerHandle(player);
        }

    }
}
