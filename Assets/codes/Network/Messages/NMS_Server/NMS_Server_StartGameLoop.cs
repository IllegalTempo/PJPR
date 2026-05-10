namespace Assets.codes.Network.Messages
{
    public class NMS_Server_StartGameLoop : NMS, IClientHandle
    {
        public NMS_Server_StartGameLoop() : base((int)packets.ServerPackets.StartGameLoop)
        {
        }

        public static NMS_Server_StartGameLoop Read(Packet packet)
        {
            return new NMS_Server_StartGameLoop();
        }

        public override void Write(Packet packet)
        {
        }

        public void ClientHandle()
        {
        }
    }
}
