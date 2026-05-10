namespace Assets.codes.Network.Messages
{
    public class NMS_Both_Interact : NMS, IClientHandle, IServerHandle
    {
        private readonly ulong whoInteracted;
        private readonly string decorationUid;

        public NMS_Both_Interact(ulong whoInteracted, string decorationUid) : base((int)packets.BothPackets.Interact)
        {
            this.whoInteracted = whoInteracted;
            this.decorationUid = decorationUid;
        }

        public static NMS_Both_Interact Read(Packet packet)
        {
            return new NMS_Both_Interact(packet.Readulong(), packet.ReadstringUNICODE());
        }

        public override void Write(Packet packet)
        {
            packet.Write(whoInteracted);
            packet.WriteUNICODE(decorationUid);
        }

        public void ClientHandle()
        {
            ApplyInteraction();
        }

        public void ServerHandle(NetworkPlayer p)
        {
            if (p.steamId != whoInteracted) return;
            ApplyInteraction();
            NetworkRouter.Instance.DistributeMessageToReady(this, p.steamId);


        }
        private void ApplyInteraction()
        {
            IUsable decoration = NetworkSystem.Instance.FindNetworkObject[decorationUid].GetComponent<IUsable>();
            PlayerMain who = NetworkSystem.Instance.PlayerList[whoInteracted].playerControl;
            decoration.OnInteract(who);
        }
    }
}
