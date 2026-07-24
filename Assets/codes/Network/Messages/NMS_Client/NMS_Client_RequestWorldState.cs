namespace Assets.codes.Network.Messages
{
    public class NMS_Client_RequestWorldState : NMS, IServerHandle
    {
        public NMS_Client_RequestWorldState() : base((int)packets.ClientPackets.RequestWorldState)
        {
        }

        public static NMS_Client_RequestWorldState Read(Packet packet)
        {
            return new NMS_Client_RequestWorldState();
        }

        public override void Write(Packet packet)
        {
        }

        public void ServerHandle(NetworkPlayer player)
        {
            GameInitManager.Instance?.SendWorldStateToClient(player);
        }
    }
}
