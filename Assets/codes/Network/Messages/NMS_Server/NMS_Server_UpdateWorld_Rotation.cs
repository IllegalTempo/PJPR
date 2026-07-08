using System;
using Assets.codes.system;
using Steamworks;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_UpdateWorld_Rotation : NMS, IClientHandle
    {
        private readonly Vector3 rotation;
        public NMS_Server_UpdateWorld_Rotation(Vector3 rotation) : base((int)packets.ServerPackets.UpdateWorld_Rotation)
        {
            this.rotation = rotation;
        }

        public static NMS_Server_UpdateWorld_Rotation Read(Packet packet)
        {
            return new NMS_Server_UpdateWorld_Rotation(packet.Readvector3());
        }

        public override void Write(Packet packet)
        {
            packet.Write(rotation);
        }

        public void ClientHandle()
        {
           WorldReference.Instance.SetRotation(rotation);
        }
    }
}
