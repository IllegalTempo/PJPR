using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_NetworkObjectInfo : NMS, IServerHandle, IClientHandle
    {
        private readonly string id;
        private readonly Vector3 position;
        private readonly Quaternion rotation;

        public NMS_Both_NetworkObjectInfo(string id, Vector3 position, Quaternion rotation) : base((int)packets.BothPackets.NO_Info)
        {
            this.id = id;
            this.position = position;
            this.rotation = rotation;
        }

        public static NMS_Both_NetworkObjectInfo Read(Packet packet)
        {
            return new NMS_Both_NetworkObjectInfo(packet.ReadstringUNICODE(), packet.Readvector3(), packet.Readquaternion());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(id);
            packet.Write(position);
            packet.Write(rotation);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            ((NetworkPrefab)NetworkSystem.Instance.FindNetworkIdentity[id]).SetServerMovement(position, rotation);
            NetworkRouter.Instance.DistributeMessageToReady(this, player.steamId);
        }

        public void ClientHandle()
        {
            NetworkPrefab networkObject = (NetworkPrefab)NetworkSystem.Instance.FindNetworkIdentity[id];
            if (GameCore.Instance != null && GameCore.Instance.IsLocal(networkObject.Sovereignty))
            {
                return;
            }

            networkObject.SetServerMovement(position, rotation);

        }
    }
}
