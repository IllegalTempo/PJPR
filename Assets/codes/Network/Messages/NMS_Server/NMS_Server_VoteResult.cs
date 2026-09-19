using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_VoteResult : NMS, IClientHandle
    {
        private readonly int winningMissionIndex;
        private readonly string winningMissionName;

        public NMS_Server_VoteResult(int winningMissionIndex, string winningMissionName) : base((int)packets.ServerPackets.VoteResult)
        {
            this.winningMissionIndex = winningMissionIndex;
            this.winningMissionName = winningMissionName;
        }

        public static NMS_Server_VoteResult Read(Packet packet)
        {
            return new NMS_Server_VoteResult(packet.Readint(), packet.ReadstringUNICODE());
        }

        public override void Write(Packet packet)
        {
            packet.Write(winningMissionIndex);
            packet.Write(winningMissionName);
        }

        public void ClientHandle()
        {
            Debug.Log($"[NMS_Server_VoteResult] Winner: index {winningMissionIndex}, name: {winningMissionName}");
            if (MissionProjectionDisplay.Instance != null)
            {
                MissionProjectionDisplay.Instance.ShowVoteResult(winningMissionIndex, winningMissionName);
            }
        }
    }
}
