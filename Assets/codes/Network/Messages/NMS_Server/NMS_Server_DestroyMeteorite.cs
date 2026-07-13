using UnityEngine;

namespace Assets.codes.Network.Messages
{
    /// Server tells clients to return a meteorite to the pool.
    public class NMS_Server_DestroyMeteorite : NMS, IClientHandle
    {
        private readonly string poolKey;
        private readonly int spawnID;

        public NMS_Server_DestroyMeteorite(string poolKey, int spawnID)
            : base((int)packets.ServerPackets.DestroyMeteorite)
        {
            this.poolKey = poolKey;
            this.spawnID = spawnID;
        }

        public static NMS_Server_DestroyMeteorite Read(Packet packet)
        {
            return new NMS_Server_DestroyMeteorite(
                packet.ReadstringUNICODE(),
                packet.Readint()
            );
        }

        public override void Write(Packet packet)
        {
            packet.Write(poolKey);
            packet.Write(spawnID);
        }

        public void ClientHandle()
        {
            if (MeteoritePool.Instance == null)
            {
                Debug.LogWarning("[NMS_Server_DestroyMeteorite] MeteoritePool.Instance is null.");
                return;
            }
        }
    }
}
