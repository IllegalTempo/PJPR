using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    public class NMS_Client_CastVote : NMS, IServerHandle
    {
        private readonly int missionIndex;

        public NMS_Client_CastVote(int missionIndex) : base((int)packets.ClientPackets.CastVote)
        {
            this.missionIndex = missionIndex;
        }

        public static NMS_Client_CastVote Read(Packet packet)
        {
            return new NMS_Client_CastVote(packet.Readint());
        }

        public override void Write(Packet packet)
        {
            packet.Write(missionIndex);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            Debug.Log($"[NMS_Client_CastVote] Player {player.SteamName} voted for mission index {missionIndex}.");
            MissionManager.Instance.CastVote(player.steamId, missionIndex);
        }
    }
}
