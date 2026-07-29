using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_VoicePacket : NMS_BOTH_SERVERACTION
    {
        private readonly byte[] data;
        private readonly ulong playerid; 
        private readonly Vector3 sendPosition;
        private readonly Vector3 sendDirection;


        public NMS_Both_VoicePacket(byte[] data,ulong playerid,Vector3 sendPos,Vector3 sendDir) : base((int)packets.BothPackets.VoicePacket)
        {
            this.data = data;
            this.playerid = playerid;
            this.sendPosition = sendPos;
            this.sendDirection = sendDir;
        }

        public static NMS_Both_VoicePacket Read(Packet packet)
        {
            return new NMS_Both_VoicePacket(packet.ReadBytesArray(),packet.Readulong(),packet.Readvector3() ,packet.Readvector3());
        }

        public override void Write(Packet packet)
        {
            packet.Write(data);
            packet.Write(playerid);
            packet.Write(sendPosition);
            packet.Write(sendDirection);
        }

        protected override void serverAction()
        {
        }

        protected override void applyaction()
        {
            NetworkPlayerObject player = NetworkSystem.Instance.GetPlayer(playerid);
            if (player != null)
            {
                //player.playerControl.ReceiveVoice(data);
                GameCore.Instance.vc.SpawnVCBubbleForLocal(new voicechat.VoiceBubble(sendPosition, sendDirection, player.playerControl), data);
            }
        }
    }
}
