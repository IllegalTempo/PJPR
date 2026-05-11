using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_PickUpItem : NMS, IServerHandle, IClientHandle
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

        private void ApplyEffect()
        {
            if(NetworkSystem.Instance.FindNetworkObject.ContainsKey(itemId))
            {
                NetworkSystem.Instance.FindNetworkObject[itemId].gameObject.GetComponent<Item>().Network_onPickUPorDrop(pickedUpBy);

            } else
            {
                throw new NO_Not_Found(itemId);
            }

        }
        public void ServerHandle(NetworkPlayer player)
        {
            if (!NetworkSystem.Instance.FindNetworkObject.TryGetValue(itemId, out NetworkObject networkObject))
            {
                throw new NO_Not_Found(itemId);
            }

            bool isDropRequest = pickedUpBy == 0;
            if (isDropRequest)
            {
                if (networkObject.Owner != player.steamId)
                {
                    Debug.LogWarning($"Rejected drop for {itemId}: {player.steamId} does not own it.");
                    return;
                }
            }
            else if (player.steamId != pickedUpBy)
            {
                Debug.LogWarning($"Rejected pickup for {itemId}: sender {player.steamId} tried to set owner {pickedUpBy}.");
                return;
            }

            ApplyEffect();
            NetworkRouter.Instance.DistributeMessage(0, this);
        }

        public void ClientHandle()
        {
            ApplyEffect();

        }
    }
}
