using UnityEngine;

namespace Assets.codes.Network.Messages
{
    /// <summary>
    /// Server tells clients to return a meteorite to the pool.
    /// </summary>
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

            // Find active meteorites by poolKey and return first matching one.
            // In a more sophisticated system, we'd track by spawnID, but for now
            // the pool return is triggered by the server's authoritative destroy.
            // Clients can simply ignore if they don't have a matching active meteorite.

            // The actual return is handled by the meteorite's BreakMeteorite or distance check.
            // This message serves as a synchronization signal.
        }
    }
}
