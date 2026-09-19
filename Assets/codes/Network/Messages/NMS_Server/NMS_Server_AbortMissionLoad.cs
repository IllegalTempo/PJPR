namespace Assets.codes.Network.Messages
{
    public sealed class NMS_Server_AbortMissionLoad : NMS, IClientHandle
    {
        public int SessionId { get; }
        public string SceneName { get; }
        public string Reason { get; }

        public NMS_Server_AbortMissionLoad(int sessionId, string sceneName, string reason)
            : base((int)packets.ServerPackets.AbortMissionLoad)
        {
            SessionId = sessionId;
            SceneName = sceneName ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public static NMS_Server_AbortMissionLoad Read(Packet packet) =>
            new(packet.Readint(), packet.ReadstringUNICODE(), packet.ReadstringUNICODE());

        public override void Write(Packet packet)
        {
            packet.Write(SessionId);
            packet.Write(SceneName);
            packet.Write(Reason);
        }

        public async void ClientHandle()
        {
            if (MissionManager.Instance != null)
                await MissionManager.Instance.HandleAbortMissionLoadAsync(SessionId, SceneName, Reason);
        }
    }
}
