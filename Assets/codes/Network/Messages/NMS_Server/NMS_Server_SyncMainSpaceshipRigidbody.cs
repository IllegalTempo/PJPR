using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SyncMainSpaceshipRigidbody : NMS, IClientHandle
    {
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly Vector3 velocity;
        private readonly Vector3 angularVelocity;
        private readonly uint tick;

        public NMS_Server_SyncMainSpaceshipRigidbody(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity, uint tick) : base((int)packets.ServerPackets.SyncMainSpaceshipRigidbody)
        {
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
            this.angularVelocity = angularVelocity;
            this.tick = tick;
        }

        public static NMS_Server_SyncMainSpaceshipRigidbody Read(Packet packet)
        {
            return new NMS_Server_SyncMainSpaceshipRigidbody(packet.Readvector3(), packet.Readquaternion(), packet.Readvector3(), packet.Readvector3(), packet.Readuint());
        }

        public override void Write(Packet packet)
        {
            packet.Write(position);
            packet.Write(rotation);
            packet.Write(velocity);
            packet.Write(angularVelocity);
            packet.Write(tick);
        }

        public void ClientHandle()
        {
            MainSpaceship.Instance?.ApplyNetworkRigidbodyState(position, rotation, velocity, angularVelocity, tick);
        }
    }
}
