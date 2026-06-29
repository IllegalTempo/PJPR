using System;
using Steamworks;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_SlotDetach : NMS_BOTH_SHARE
    {
        private readonly string SlotId;
        public NMS_Both_SlotDetach(string SlotId) : base((int)packets.BothPackets.SlotDetach)
        {
            this.SlotId = SlotId;
        }

        public static NMS_Both_SlotDetach Read(Packet packet)
        {
            return new NMS_Both_SlotDetach(packet.ReadstringUNICODE());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(SlotId);
        }

        protected override void applyaction()
        {
            NetworkSystem.Instance.GetComponentOfIdentity<Slot>(SlotId).Detach();
        }
    }
}
