namespace Assets.codes.Network.Messages
{
    public sealed class NMS_Server_LoadMissionScene : NMS, IClientHandle
    {
        public int SessionId { get; }
        public string MissionName { get; }
        public string SceneName { get; }

        public NMS_Server_LoadMissionScene(int sessionId, string missionName, string sceneName)
            : base((int)packets.ServerPackets.LoadMissionScene)
        {
            SessionId = sessionId;
            MissionName = missionName ?? string.Empty;
            SceneName = sceneName ?? string.Empty;
        }

        public static NMS_Server_LoadMissionScene Read(Packet packet) =>
            new(packet.Readint(), packet.ReadstringUNICODE(), packet.ReadstringUNICODE());

        public override void Write(Packet packet)
        {
            packet.Write(SessionId);
            packet.Write(MissionName);
            packet.Write(SceneName);
        }

        public async void ClientHandle()
        {
            if (MissionManager.Instance != null)
                await MissionManager.Instance.HandleLoadMissionSceneAsync(SessionId, MissionName, SceneName);
        }
    }
}
