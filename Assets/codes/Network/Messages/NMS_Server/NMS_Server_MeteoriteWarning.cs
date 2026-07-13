using UnityEngine;

namespace Assets.codes.Network.Messages
{
    /// <summary>
    /// Server tells clients to show a meteorite warning indicator.
    /// </summary>
    public class NMS_Server_MeteoriteWarning : NMS, IClientHandle
    {
        private readonly Vector3 direction;
        private readonly float duration;
        private readonly int warningID;

        public NMS_Server_MeteoriteWarning(Vector3 direction, float duration, int warningID)
            : base((int)packets.ServerPackets.MeteoriteWarning)
        {
            this.direction = direction;
            this.duration = duration;
            this.warningID = warningID;
        }

        public static NMS_Server_MeteoriteWarning Read(Packet packet)
        {
            return new NMS_Server_MeteoriteWarning(
                packet.Readvector3(),
                packet.Readfloat(),
                packet.Readint()
            );
        }

        public override void Write(Packet packet)
        {
            packet.Write(direction);
            packet.Write(duration);
            packet.Write(warningID);
        }

        public void ClientHandle()
        {
            if (MeteoritePool.Instance == null)
            {
                Debug.LogWarning("[NMS_Server_MeteoriteWarning] MeteoritePool.Instance is null.");
                return;
            }

            // Get a warning indicator from the pool and show it
            GameObject warningObj = MeteoritePool.Instance.Get("Warning", Vector3.zero, Quaternion.identity);
            if (warningObj != null)
            {
                MeteoriteWarningIndicator indicator = warningObj.GetComponent<MeteoriteWarningIndicator>();
                if (indicator != null)
                {
                    indicator.Show(direction, duration);
                }
                else
                {
                    MeteoritePool.Instance.Return(warningObj, "Warning");
                }
            }
        }
    }
}
