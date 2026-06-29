namespace Assets.codes.Network.Messages
{
    public class NMS_Both_Interact : NMS_BOTH_SHARE
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


        public override void ServerHandle(NetworkPlayer p)
        {
            if (p.steamId != whoInteracted) return;
            base.ServerHandle(p);


        }

        protected override void applyaction()
        {
            IUsable decoration = NetworkSystem.Instance.GetComponentOfIdentity<IUsable>(decorationUid);
            PlayerMain who = NetworkSystem.Instance.PlayerList[whoInteracted].playerControl;
            decoration.OnInteract(who);
        }
    }
}
