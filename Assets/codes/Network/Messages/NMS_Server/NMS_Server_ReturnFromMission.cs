using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public sealed class NMS_Server_ReturnFromMission : NMS, IClientHandle
    {
        public int SessionId { get; }
        public Vector3 ReturnPosition { get; }
        public Quaternion ReturnRotation { get; }
        public bool Succeeded { get; }

        public NMS_Server_ReturnFromMission(int sessionId, Vector3 returnPosition, Quaternion returnRotation, bool succeeded)
            : base((int)packets.ServerPackets.ReturnFromMission)
        {
            SessionId = sessionId;
            ReturnPosition = returnPosition;
            ReturnRotation = returnRotation;
            Succeeded = succeeded;
        }

        public static NMS_Server_ReturnFromMission Read(Packet packet) =>
            new(packet.Readint(), packet.Readvector3(), packet.Readquaternion(), packet.Readbool());

        public override void Write(Packet packet)
        {
            packet.Write(SessionId);
            packet.Write(ReturnPosition);
            packet.Write(ReturnRotation);
            packet.Write(Succeeded);
        }

        public async void ClientHandle()
        {
            if (MissionManager.Instance != null)
                await MissionManager.Instance.HandleReturnFromMissionAsync(SessionId, ReturnPosition, ReturnRotation, Succeeded);
        }
    }
}
