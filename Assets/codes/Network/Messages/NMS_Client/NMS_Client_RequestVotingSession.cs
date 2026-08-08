using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    public class NMS_Client_RequestVotingSession : NMS_BOTH_SERVERACTION
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


        protected override void serverAction()
        {
            Debug.Log($"[NMS_Client_RequestVotingSession] Received request for voting session from terminal {terminalNetworkObjectId} with {missionCount} missions.");
            MissionManager.Instance.StartVotingSession(missionCount);
        }

        protected override void applyaction()
        {
        }
    }
}
