using System;
using Steamworks;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_SyncNetworkVariable : NMS_BOTH_SHARE
    {
        public NMS_Both_SyncNetworkVariable() : base((int)packets.BothPackets.SyncNetworkVariable)
        {
        }

        public static NMS_Both_SyncNetworkVariable Read(Packet packet)
        {
            return new NMS_Both_SyncNetworkVariable();
        }

        public override void Write(Packet packet)
        {
            // No fields to write.
        }

        protected override void applyaction()
        {
            // TODO: Apply shared client/server behavior.
        }
    }
}
