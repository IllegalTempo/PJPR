using System;
using Assets.codes.machines;
using Steamworks;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_MachineInteract : NMS_BOTH_SERVERACTION
    {
        private readonly string MachineID;
        public NMS_Both_MachineInteract (string MachineID) : base((int)packets.BothPackets.QuantityResourceProviderInteract)
        {
            this.MachineID = MachineID;
        }

        public static NMS_Both_MachineInteract Read(Packet packet)
        {
            return new NMS_Both_MachineInteract(packet.ReadstringUNICODE());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(MachineID);
        }

        protected override void applyaction()
        {
            Machine PacketReferencedMachine = NetworkSystem.Instance.GetComponentOfIdentity<Machine>(MachineID);
            PacketReferencedMachine.ShareActionOnInteract();
        }

        protected override void serverAction()
        {
            Machine PacketReferencedMachine = NetworkSystem.Instance.GetComponentOfIdentity<Machine>(MachineID);

            PacketReferencedMachine.ServerActionOnInteract();
        }
    }
}
