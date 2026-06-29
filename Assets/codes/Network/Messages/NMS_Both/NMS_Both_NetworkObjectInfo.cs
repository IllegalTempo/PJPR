using Assets.codes.Network.SyncedIdentity;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_NetworkObjectInfo : NMS_BOTH_SHARE
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


        public override void ClientHandle()
        {
            NetworkGameObject networkObject = NetworkSystem.Instance.GetComponentOfIdentity<NetworkGameObject>(id);
            if (GameCore.Instance != null && networkObject.IsLocalSovereignty())
            {
                return;
            }
            base.ClientHandle();
        }

        protected override void applyaction()
        {
            NetworkSystem.Instance.GetComponentOfIdentity<NetworkGameObject>(id).SetServerMovement(position, rotation);
        }
    }
}
