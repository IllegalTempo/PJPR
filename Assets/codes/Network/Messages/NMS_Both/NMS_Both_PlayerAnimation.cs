namespace Assets.codes.Network.Messages
{
    public class NMS_Both_PlayerAnimation : NMS, IClientHandle, IServerHandle
    {
        private readonly ulong playerId;
        private readonly float movementX;
        private readonly float movementY;

        public NMS_Both_PlayerAnimation(ulong playerId, float movementX, float movementY) : base((int)packets.BothPackets.PlayerAnimation)
        {
            this.playerId = playerId;
            this.movementX = movementX;
            this.movementY = movementY;
        }

        public static NMS_Both_PlayerAnimation Read(Packet packet)
        {
            return new NMS_Both_PlayerAnimation(packet.Readulong(), packet.Readfloat(), packet.Readfloat());
        }

        public override void Write(Packet packet)
        {
            packet.Write(playerId);
            packet.Write(movementX);
            packet.Write(movementY);
        }

        public void ClientHandle()
        {
            NetworkSystem.Instance.PlayerList[playerId].SetAnimation(movementX, movementY);
        }

        public void ServerHandle(NetworkPlayer p)
        {
            if (p.steamId != playerId) return;
            p.player.SetAnimation(movementX, movementY);
            NetworkRouter.Instance.DistributeMessageToReady(this, p.steamId);
        }
    }
}
