using System;
using Assets.codes.system;
using Steamworks;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_UpdateWorld_Velocity : NMS, IClientHandle
    {
        private readonly Vector3 velocity;
        public NMS_Server_UpdateWorld_Velocity(Vector3 velocity) : base((int)packets.ServerPackets.UpdateWorld_Velocity)
        {
            this.velocity = velocity;
        }

        public static NMS_Server_UpdateWorld_Velocity Read(Packet packet)
        {
            return new NMS_Server_UpdateWorld_Velocity(packet.Readvector3());
        }

        public override void Write(Packet packet)
        {
            packet.Write(velocity);
        }

        public void ClientHandle()
        {
            WorldReference.Instance.SetVelocity(velocity);
        }
    }
}
