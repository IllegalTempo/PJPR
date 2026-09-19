namespace Assets.codes.Network.Messages
{
    public sealed class NMS_Client_MissionSceneReady : NMS, IServerHandle
    {
        public int SessionId { get; }
        public string SceneName { get; }
        public bool RequiresEntryCatchup { get; }

        public NMS_Client_MissionSceneReady(int sessionId, string sceneName, bool requiresEntryCatchup = true)
            : base((int)packets.ClientPackets.MissionSceneReady)
        {
            SessionId = sessionId;
            SceneName = sceneName ?? string.Empty;
            RequiresEntryCatchup = requiresEntryCatchup;
        }

        public static NMS_Client_MissionSceneReady Read(Packet packet) =>
            new(packet.Readint(), packet.ReadstringUNICODE(), packet.Readbool());

        public override void Write(Packet packet)
        {
            packet.Write(SessionId);
            packet.Write(SceneName);
            packet.Write(RequiresEntryCatchup);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            MissionManager.Instance?.HandleMissionSceneReady(
                player.steamId, SessionId, SceneName, RequiresEntryCatchup);
        }
    }
}
