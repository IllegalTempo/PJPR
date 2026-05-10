using Cysharp.Threading.Tasks;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_NewPlayerJoined : NMS, IClientHandle
    {
        private readonly ulong playerId;

        public NMS_Server_NewPlayerJoined(ulong playerId) : base((int)packets.ServerPackets.UpdatePlayerEnterRoomForExistingPlayer)
        {
            this.playerId = playerId;
        }

        public static NMS_Server_NewPlayerJoined Read(Packet packet)
        {
            return new NMS_Server_NewPlayerJoined(packet.Readulong());
        }

        public override void Write(Packet packet)
        {
            packet.Write(playerId);
        }

        public void ClientHandle()
        {
            NetworkSystem.Instance.Client.NewPlayer(playerId).Forget();
        }
    }
}
