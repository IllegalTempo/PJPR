using Assets.codes.Network.Packets.BothMessages;
using Steamworks;
using System.Collections;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_PositionUpdate : NMS_BOTH_SHARE
    {
        ulong SourceNetworkID;
        Vector3 pos;
        Quaternion headrot;
        Quaternion bodyrot;
        public NMS_Both_PositionUpdate(ulong SourceNetworkID,Vector3 pos,Quaternion headrot, Quaternion bodyrot) : base((int)packets.BothPackets.PosUpdate)
        {
            this.SourceNetworkID = SourceNetworkID;
            this.pos = pos;
            this.headrot = headrot;
            this.bodyrot = bodyrot;
        }
        public static NMS_Both_PositionUpdate Read(Packet packet)
        {
            ulong steamID = packet.Readulong();

            Vector3 pos = packet.Readvector3();
            Quaternion headrot = packet.Readquaternion();
            Quaternion bodyrot = packet.Readquaternion();
            return new NMS_Both_PositionUpdate(steamID, pos, headrot, bodyrot);
        }

        public override void ServerHandle(NetworkPlayer p)
        {
            if (p.steamId != SourceNetworkID) return;
            
            base.ServerHandle(p);

        }

        public override void Write(Packet p)
        {
            p.Write(SourceNetworkID);
            p.Write(pos);
            p.Write(headrot);
            p.Write(bodyrot);
        }

        protected override void applyaction()
        {
            NetworkSystem.Instance.PlayerList[SourceNetworkID].SetMovement(pos, headrot, bodyrot);
        }
    }
}
