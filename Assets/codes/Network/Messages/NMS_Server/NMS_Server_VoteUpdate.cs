using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_VoteUpdate : NMS, IClientHandle
    {
        private readonly int missionCount;
        private readonly int[] voteCounts;

        public NMS_Server_VoteUpdate(int[] voteCounts) : base((int)packets.ServerPackets.VoteUpdate)
        {
            missionCount = voteCounts.Length;
            this.voteCounts = voteCounts;
        }

        public static NMS_Server_VoteUpdate Read(Packet packet)
        {
            int count = packet.Readint();
            int[] counts = new int[count];
            for (int i = 0; i < count; i++)
                counts[i] = packet.Readint();
            return new NMS_Server_VoteUpdate(counts);
        }

        public override void Write(Packet packet)
        {
            packet.Write(missionCount);
            for (int i = 0; i < missionCount; i++)
                packet.Write(voteCounts[i]);
        }

        public void ClientHandle()
        {
            if (MissionProjectionDisplay.Instance != null)
            {
                MissionProjectionDisplay.Instance.UpdateVoteCounts(voteCounts);
            }
        }
    }
}
