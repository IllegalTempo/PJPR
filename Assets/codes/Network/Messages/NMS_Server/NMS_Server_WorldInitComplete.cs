namespace Assets.codes.Network.Messages
{
    public class NMS_Server_WorldInitComplete : NMS, IClientHandle
    {
        public NMS_Server_WorldInitComplete() : base((int)packets.ServerPackets.WorldInitComplete)
        {
        }

        public static NMS_Server_WorldInitComplete Read(Packet packet)
        {
            return new NMS_Server_WorldInitComplete();
        }

        public override void Write(Packet packet)
        {
        }

        public void ClientHandle()
        {
            GameInitManager.Instance?.NotifyWorldInitComplete();
        }
    }
}
