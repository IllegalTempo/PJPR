using System.Collections;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_VoicePacket : NMS, IClientHandle, IServerHandle
    {
        private readonly byte[] data;
        private readonly ulong playerid; 

        public NMS_Both_VoicePacket(byte[] data,ulong playerid) : base((int)packets.BothPackets.VoicePacket)
        {
            this.data = data;
            this.playerid = playerid;
        }

        public static NMS_Both_VoicePacket Read(Packet packet)
        {
            return new NMS_Both_VoicePacket(packet.ReadBytesArray(),packet.Readulong());
        }

        public override void Write(Packet packet)
        {
            packet.Write(data);
            packet.Write(playerid);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            if (playerid != player.steamId) return;
            player.player.playerControl.ReceiveVoice(data);
            NetworkRouter.Instance.DistributeMessageToReady(this, player.steamId, NetworkSendProfiles.Voice);
        }

        public void ClientHandle()
        {
            if (NetworkSystem.Instance.PlayerList.TryGetValue(playerid, out NetworkPlayerObject player))
            {
                player.playerControl.ReceiveVoice(data);
            }

        }
    }
}
