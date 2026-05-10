namespace Assets.codes.Network.Messages
{
    public class NMS_Server_MissionInfo : NMS, IClientHandle
    {
        private readonly int missionLevel;
        private readonly int missionIndex;

        public NMS_Server_MissionInfo(int missionLevel, int missionIndex) : base((int)packets.ServerPackets.SendMissionInfo)
        {
            this.missionLevel = missionLevel;
            this.missionIndex = missionIndex;
        }

        public static NMS_Server_MissionInfo Read(Packet packet)
        {
            return new NMS_Server_MissionInfo(packet.Readint(), packet.Readint());
        }

        public override void Write(Packet packet)
        {
            packet.Write(missionLevel);
            packet.Write(missionIndex);
        }

        public void ClientHandle()
        {
        }
    }
}
