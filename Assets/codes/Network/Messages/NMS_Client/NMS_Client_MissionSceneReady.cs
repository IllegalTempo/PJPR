namespace Assets.codes.Network.Messages
{
    public sealed class NMS_Client_MissionSceneReady : NMS, IServerHandle
    {
        public int SessionId { get; }
        public string SceneName { get; }

        public NMS_Client_MissionSceneReady(int sessionId, string sceneName)
            : base((int)packets.ClientPackets.MissionSceneReady)
        {
            SessionId = sessionId;
            SceneName = sceneName ?? string.Empty;
        }

        public static NMS_Client_MissionSceneReady Read(Packet packet) =>
            new(packet.Readint(), packet.ReadstringUNICODE());

        public override void Write(Packet packet)
        {
            packet.Write(SessionId);
            packet.Write(SceneName);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            MissionManager.Instance?.HandleMissionSceneReady(player.steamId, SessionId, SceneName);
        }
    }
}
