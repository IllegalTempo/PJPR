using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    /// <summary>
    /// Server -> Client. Reliable event for charge lifecycle moments that should be
    /// immediate and not wait for the next state tick (started / completed / failed).
    /// </summary>
    public class NMS_Server_PeakOfEnergyChargeEvent : NMS, IClientHandle
    {
        public enum ChargeEventType
        {
            Started = 0,
            Completed = 1,
            Failed = 2,
        }

        private readonly int eventType;
        private readonly int chargeIndex;

        public NMS_Server_PeakOfEnergyChargeEvent(int eventType, int chargeIndex)
            : base((int)packets.ServerPackets.PeakOfEnergyChargeEvent)
        {
            this.eventType = eventType;
            this.chargeIndex = chargeIndex;
        }

        public static NMS_Server_PeakOfEnergyChargeEvent Read(Packet packet)
        {
            return new NMS_Server_PeakOfEnergyChargeEvent(packet.Readint(), packet.Readint());
        }

        public override void Write(Packet packet)
        {
            packet.Write(eventType);
            packet.Write(chargeIndex);
        }

        public void ClientHandle()
        {
            if (PeakOfEnergyManager.Instance == null)
            {
                Debug.LogWarning("[NMS_Server_PeakOfEnergyChargeEvent] No PeakOfEnergyManager in scene.");
                return;
            }

            PeakOfEnergyManager.Instance.HandleChargeEvent((ChargeEventType)eventType, chargeIndex);
        }
    }
}
