namespace Assets.codes.Network.Messages
{
    public class NMS_Server_WorldInitBegin : NMS, IClientHandle
    {
        public NMS_Server_WorldInitBegin() : base((int)packets.ServerPackets.WorldInitBegin)
        {
        }

        public static NMS_Server_WorldInitBegin Read(Packet packet)
        {
            return new NMS_Server_WorldInitBegin();
        }

        public override void Write(Packet packet)
        {
        }

        public void ClientHandle()
        {
            GameInitManager.Instance?.NotifyWorldInitBegin();
        }
    }
}
