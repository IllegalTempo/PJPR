

using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_SlotAttach : NMS_BOTH_SHARE
    {
        private readonly string SlotId; //slot id is now the name of the slot gameobject as there will be no extra slot added to the game, if this is the case, put networkobject into slots
        private readonly string ItemId;

        private readonly Quaternion installedRotation;
        public NMS_Both_SlotAttach(string slotId,string itemID,Quaternion rot) : base((int)packets.BothPackets.NO_Slot_Interact)
        {
            this.SlotId = slotId;
            this.ItemId = itemID;
            this.installedRotation = rot;
        }

        public static NMS_Both_SlotAttach Read(Packet packet)
        {
            // TODO: Read message fields from packet.
            return new NMS_Both_SlotAttach(packet.ReadstringUNICODE(),packet.ReadstringUNICODE(), packet.Readquaternion());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(SlotId);
            packet.WriteUNICODE(ItemId);
            packet.Write(installedRotation);
        }

        protected override void applyaction()
        {
            Item item = NetworkSystem.Instance.GetComponentOfIdentity<Item>(ItemId);
            NetworkSystem.Instance.GetComponentOfIdentity<Slot>(SlotId).Attach(ItemId,installedRotation);
        }
    }
}
