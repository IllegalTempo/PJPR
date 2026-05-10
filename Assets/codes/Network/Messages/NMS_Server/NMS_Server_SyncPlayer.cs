using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SyncPlayer : NMS, IClientHandle
    {
        private readonly ulong[] playerIds;

        public NMS_Server_SyncPlayer(IEnumerable<ulong> playerIds) : base((int)packets.ServerPackets.RoomInfoOnPlayerEnterRoom)
        {
            this.playerIds = new List<ulong>(playerIds).ToArray();
        }

        public static NMS_Server_SyncPlayer Read(Packet packet)
        {
            int count = packet.Readint();
            ulong[] playerIds = new ulong[count];
            for (int i = 0; i < count; i++)
            {
                playerIds[i] = packet.Readulong();
            }

            return new NMS_Server_SyncPlayer(playerIds);
        }

        public override void Write(Packet packet)
        {
            packet.Write(playerIds.Length + 1);
            packet.Write(NetworkSystem.Instance.SteamID);
            foreach (ulong playerId in playerIds)
            {
                packet.Write(playerId);
            }
        }

        public async void ClientHandle()
        {
            foreach (ulong playerId in playerIds)
            {
                NetworkSystem.Instance.Client.NewPlayer(playerId).Forget();
            }

            await Task.Delay(1000);
            NetworkRouter.Instance.UpdateReadyState(ReadyState.SyncPlayer);
        }
    }
}
