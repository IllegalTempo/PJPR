using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    public class NMS_Client_RequestVotingSession : NMS, IServerHandle
    {
        private readonly string terminalNetworkObjectId;
        private readonly int missionCount;

        public NMS_Client_RequestVotingSession(string terminalId, int missionCount) : base((int)packets.ClientPackets.RequestVotingSession)
        {
            this.terminalNetworkObjectId = terminalId;
            this.missionCount = missionCount;
        }

        public static NMS_Client_RequestVotingSession Read(Packet packet)
        {
            return new NMS_Client_RequestVotingSession(packet.ReadstringUNICODE(), packet.Readint());
        }

        public override void Write(Packet packet)
        {
            packet.Write(terminalNetworkObjectId);
            packet.Write(missionCount);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            Debug.Log($"[NMS_Client_RequestVotingSession] Player {player.SteamName} requested voting session with {missionCount} missions.");
            MissionManager.Instance.StartVotingSession(missionCount);
        }
    }
}
