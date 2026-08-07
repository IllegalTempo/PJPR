using System;
using Assets.codes.machines;
using Steamworks;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_MachineInteract : NMS_BOTH_SERVERACTION
    {
        private readonly string MachineID;
        private readonly int InteractType;
        private readonly ulong PlayerID;

        public NMS_Both_MachineInteract (string MachineID, int InteractType, ulong playerid) : base((int)packets.BothPackets.QuantityResourceProviderInteract)
        {
            this.MachineID = MachineID;
            this.InteractType = InteractType;
            PlayerID = playerid;
        }

        public static NMS_Both_MachineInteract Read(Packet packet)
        {
            return new NMS_Both_MachineInteract(packet.ReadstringUNICODE(), packet.Readint(), packet.Readulong());
        }

        public override void Write(Packet packet)
        {
            packet.Write(MachineID);
            packet.Write(InteractType);
            packet.Write(PlayerID);
        }

        protected override void applyaction()
        {
            SyncedMachine PacketReferencedMachine = NetworkSystem.Instance.GetComponentOfIdentity<SyncedMachine>(MachineID);
            PacketReferencedMachine.OnNetworkApplyAction(InteractType,NetworkSystem.Instance.GetPlayer(PlayerID).playerControl);
        }

        protected override void serverAction()
        {
            SyncedMachine PacketReferencedMachine = NetworkSystem.Instance.GetComponentOfIdentity<SyncedMachine>(MachineID);

            PacketReferencedMachine.OnNetworkApplyActionServer(InteractType,NetworkSystem.Instance.GetPlayer(PlayerID).playerControl);
        }
    }
}
