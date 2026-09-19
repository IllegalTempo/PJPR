using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public sealed class NMS_Server_EnterMission : NMS, IClientHandle
    {
        public int SessionId { get; }
        public Vector3 ShipPosition { get; }
        public Quaternion ShipRotation { get; }
        public string ReturnPortalId { get; }

        public NMS_Server_EnterMission(int sessionId, Vector3 shipPosition, Quaternion shipRotation, string returnPortalId)
            : base((int)packets.ServerPackets.EnterMission)
        {
            SessionId = sessionId;
            ShipPosition = shipPosition;
            ShipRotation = shipRotation;
            ReturnPortalId = returnPortalId ?? string.Empty;
        }

        public static NMS_Server_EnterMission Read(Packet packet) =>
            new(packet.Readint(), packet.Readvector3(), packet.Readquaternion(), packet.ReadstringUNICODE());

        public override void Write(Packet packet)
        {
            packet.Write(SessionId);
            packet.Write(ShipPosition);
            packet.Write(ShipRotation);
            packet.Write(ReturnPortalId);
        }

        public void ClientHandle()
        {
            MissionManager.Instance?.HandleEnterMission(SessionId, ShipPosition, ShipRotation, ReturnPortalId);
        }
    }
}
