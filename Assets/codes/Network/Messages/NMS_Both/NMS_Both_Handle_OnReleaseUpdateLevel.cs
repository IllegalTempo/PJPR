using Assets.codes.spaceship.mechanics;
using Steamworks;
using System;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_Handle_OnReleaseUpdateLevel : NMS_BOTH_SERVERACTION
    {
        private readonly string id;
        private readonly int level;
        public NMS_Both_Handle_OnReleaseUpdateLevel(string id,int level) : base((int)packets.BothPackets.Handle_OnReleaseUpdateLevel)
        {
            this.level = level;
            this.id = id;
        }

        public static NMS_Both_Handle_OnReleaseUpdateLevel Read(Packet packet)
        {
            return new NMS_Both_Handle_OnReleaseUpdateLevel(packet.ReadstringUNICODE(), packet.Readint());
        }

        public override void Write(Packet packet)
        {
            packet.Write(id);
            packet.Write(level);
        }

        protected override void applyaction()
        {
            handlecontrol PacketReferencedMachine = NetworkSystem.Instance.GetComponentOfIdentity<handlecontrol>(id);
            PacketReferencedMachine.OnStepChanged(level);
        }

        protected override void serverAction()
        {
            handlecontrol PacketReferencedMachine = NetworkSystem.Instance.GetComponentOfIdentity<handlecontrol>(id);
            PacketReferencedMachine.OnStepChanged_Server(level);
        }
    }
}
