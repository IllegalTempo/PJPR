using UnityEngine;

namespace Assets.codes.Network.Messages
{
    /// <summary>
    /// Server tells clients to spawn a meteorite from the pool.
    /// </summary>
    public class NMS_Server_SpawnMeteorite : NMS, IClientHandle
    {
        private readonly string poolKey;
        private readonly Vector3 position;
        private readonly Vector3 velocity;
        private readonly Vector3 angularVelocity;
        private readonly float scale;
        private readonly int spawnID;

        public NMS_Server_SpawnMeteorite(string poolKey, Vector3 position, Vector3 velocity,
            Vector3 angularVelocity, float scale, int spawnID)
            : base((int)packets.ServerPackets.SpawnMeteorite)
        {
            this.poolKey = poolKey;
            this.position = position;
            this.velocity = velocity;
            this.angularVelocity = angularVelocity;
            this.scale = scale;
            this.spawnID = spawnID;
        }

        public static NMS_Server_SpawnMeteorite Read(Packet packet)
        {
            return new NMS_Server_SpawnMeteorite(
                packet.ReadstringUNICODE(),
                packet.Readvector3(),
                packet.Readvector3(),
                packet.Readvector3(),
                packet.Readfloat(),
                packet.Readint()
            );
        }

        public override void Write(Packet packet)
        {
            packet.Write(poolKey);
            packet.Write(position);
            packet.Write(velocity);
            packet.Write(angularVelocity);
            packet.Write(scale);
            packet.Write(spawnID);
        }

        public void ClientHandle()
        {
            if (MeteoritePool.Instance == null)
            {
                Debug.LogWarning("[NMS_Server_SpawnMeteorite] MeteoritePool.Instance is null.");
                return;
            }

            Quaternion rotation = velocity.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(velocity.normalized)
                : Quaternion.identity;

            GameObject obj = MeteoritePool.Instance.Get(poolKey, position, rotation);
            if (obj == null)
            {
                Debug.LogWarning($"[NMS_Server_SpawnMeteorite] Failed to get '{poolKey}' from pool on client.");
                return;
            }

            obj.transform.localScale = Vector3.one * scale;

            Meteorite meteorite = obj.GetComponent<Meteorite>();
            if (meteorite != null)
            {
                meteorite.poolKey = poolKey;
                // On client, returning to pool doesn't need to broadcast since server is authoritative
                meteorite.onReturnToPool = (m) =>
                {
                    MeteoritePool.Instance.Return(m.gameObject, m.poolKey);
                };
            }

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = velocity;
                rb.angularVelocity = angularVelocity;
            }
        }
    }
}
