namespace Assets.codes.Network.Messages
{
    public class NMS_Both_SlotAttach : NMS, IClientHandle, IServerHandle
    {
        public string SlotId; //slot id is now the name of the slot gameobject as there will be no extra slot added to the game, if this is the case, put networkobject into slots
        public string ItemId;
        public NMS_Both_SlotAttach(string slotId,string itemID) : base((int)packets.BothPackets.NO_Slot_Interact)
        {
            this.SlotId = slotId;
            this.ItemId = itemID;
        }

        public static NMS_Both_SlotAttach Read(Packet packet)
        {
            // TODO: Read message fields from packet.
            return new NMS_Both_SlotAttach(packet.ReadstringUNICODE(),packet.ReadstringUNICODE());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(SlotId);
            packet.WriteUNICODE(ItemId);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            process();
            NetworkRouter.Instance.DistributeMessageToReady(this, player.steamId);
        }

        public void ClientHandle()
        {
            process();
        }
        private void process()
        {
            Item item = NetworkSystem.Instance.GetComponentOfIdentity<Item>(ItemId);
            NetworkSystem.Instance.GetComponentOfIdentity<Slot>(SlotId).Attach(ItemId);
        }
    }
}
