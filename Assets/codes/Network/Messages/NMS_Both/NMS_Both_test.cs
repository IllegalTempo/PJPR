namespace Assets.codes.Network.Messages
{
    public class NMS_Both_test : NMS, IClientHandle, IServerHandle
    {
        public NMS_Both_test() : base((int)packets.BothPackets.test)
        {
        }

        public static NMS_Both_test Read(Packet packet)
        {
            // TODO: Read message fields from packet.
            return new NMS_Both_test();
        }

        public override void Write(Packet packet)
        {
            // TODO: Write message fields to packet.
        }

        public void ServerHandle(NetworkPlayer player)
        {
            // TODO: Apply server-side behavior.
            // NetworkRouter.Instance.DistributeMessageToReady(this, player.steamId);
        }

        public void ClientHandle()
        {
            // TODO: Apply client-side behavior.
        }
    }
}
