using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_NetworkObjectActive : NMS, IServerHandle, IClientHandle
    {
        private readonly string id;
        private readonly bool active;

        public NMS_Both_NetworkObjectActive(string id, bool active) : base((int)packets.BothPackets.NO_Active)
        {
            this.id = id;
            this.active = active;

        }

        public static NMS_Both_NetworkObjectActive Read(Packet packet)
        {
            return new NMS_Both_NetworkObjectActive(packet.ReadstringUNICODE(), packet.Readbool());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(id);
            packet.Write(active);
        }
        private void ApplyEffect()
        {
            if (NetworkSystem.Instance.FindNetworkObject.ContainsKey(id))
            {
                NetworkSystem.Instance.FindNetworkObject[id].gameObject.SetActive(active);

            }
            else
            {
                throw new NO_Not_Found(id);
            }
        }
        public void ServerHandle(NetworkPlayer player)
        {
            ApplyEffect();
            NetworkRouter.Instance.DistributeMessageToReady(this);
        }

        public void ClientHandle()
        {
            ApplyEffect();

        }
    }
}
