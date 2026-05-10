namespace Assets.codes.Network.Messages
{
    public class NMS_Server_PlayerQuit : NMS, IClientHandle
    {
        private readonly ulong playerId;

        public NMS_Server_PlayerQuit(ulong playerId) : base((int)packets.ServerPackets.PlayerQuit)
        {
            this.playerId = playerId;
        }

        public static NMS_Server_PlayerQuit Read(Packet packet)
        {
            return new NMS_Server_PlayerQuit(packet.Readulong());
        }

        public override void Write(Packet packet)
        {
            packet.Write(playerId);
        }

        public void ClientHandle()
        {
            NetworkSystem.Instance.Client.PlayerQuit(playerId);
        }
    }
}
