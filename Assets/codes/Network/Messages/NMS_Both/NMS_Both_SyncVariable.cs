using System;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_SyncVariable : NMS, IClientHandle, IServerHandle
    {
        private readonly string networkObjectId;
        private readonly int variableId;
        private readonly byte[] valueBytes;

        public NMS_Both_SyncVariable() : this(string.Empty, 0, Array.Empty<byte>())
        {
        }

        public NMS_Both_SyncVariable(string networkObjectId, int variableId, byte[] valueBytes) : base((int)packets.BothPackets.SyncVariable)
        {
            this.networkObjectId = networkObjectId;
            this.variableId = variableId;
            this.valueBytes = valueBytes ?? Array.Empty<byte>();
        }

        public static NMS_Both_SyncVariable Read(Packet packet)
        {
            return new NMS_Both_SyncVariable(packet.ReadstringUNICODE(), packet.Readint(), packet.ReadBytesArray());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(networkObjectId);
            packet.Write(variableId);
            packet.Write(valueBytes);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            if (!NetworkSystem.Instance.FindNetworkObject.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                throw new NO_Not_Found(networkObjectId);
            }

            if (!networkObject.CanClientSyncVariable(variableId, player))
            {
                return;
            }

            if (!networkObject.ApplySyncedVariable(variableId, valueBytes))
            {
                return;
            }

            NetworkRouter.Instance.DistributeMessageToReady(this, player.steamId);
        }

        public void ClientHandle()
        {
            if (!NetworkSystem.Instance.FindNetworkObject.TryGetValue(networkObjectId, out NetworkObject networkObject))
            {
                throw new NO_Not_Found(networkObjectId);
            }

            networkObject.ApplySyncedVariable(variableId, valueBytes);
        }
    }
}
