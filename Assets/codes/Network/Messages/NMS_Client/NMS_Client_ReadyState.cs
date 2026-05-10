namespace Assets.codes.Network.Messages
{
    public class NMS_Client_ReadyState : NMS, IServerHandle
    {
        private readonly int state;

        public NMS_Client_ReadyState(int state) : base((int)packets.ClientPackets.SendReadyState)
        {
            this.state = state;
        }

        public static NMS_Client_ReadyState Read(Packet packet)
        {
            return new NMS_Client_ReadyState(packet.Readint());
        }

        public override void Write(Packet packet)
        {
            packet.Write(state);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            player.ReadyState = state;
            NetworkSystem.Instance.NetworkListener.RaiseReadyState(player, state);
        }
    }
}
