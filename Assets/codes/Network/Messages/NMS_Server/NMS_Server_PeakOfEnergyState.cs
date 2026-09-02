using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    /// <summary>
    /// Server -> Client. Periodic authoritative snapshot of the Peak Of Energy mission
    /// (RPM, stability, health, charge state, cooldown, wobble). Clients mirror these
    /// values into their local PeakOfEnergyManager so visuals/UI react the same everywhere.
    /// </summary>
    public class NMS_Server_PeakOfEnergyState : NMS, IClientHandle
    {
        private readonly float rpm;
        private readonly bool isStable;
        private readonly int state;
        private readonly int health;
        private readonly int chargeIndex;
        private readonly float cooldownRemaining;
        private readonly bool isCharging;
        private readonly float chargeProgress;
        private readonly float wobble;
        private readonly float targetMin;
        private readonly float targetMax;

        public NMS_Server_PeakOfEnergyState(
            float rpm, bool isStable, int state, int health, int chargeIndex,
            float cooldownRemaining, bool isCharging, float chargeProgress,
            float wobble, float targetMin, float targetMax)
            : base((int)packets.ServerPackets.PeakOfEnergyState)
        {
            this.rpm = rpm;
            this.isStable = isStable;
            this.state = state;
            this.health = health;
            this.chargeIndex = chargeIndex;
            this.cooldownRemaining = cooldownRemaining;
            this.isCharging = isCharging;
            this.chargeProgress = chargeProgress;
            this.wobble = wobble;
            this.targetMin = targetMin;
            this.targetMax = targetMax;
        }

        public static NMS_Server_PeakOfEnergyState Read(Packet packet)
        {
            return new NMS_Server_PeakOfEnergyState(
                packet.Readfloat(),
                packet.Readbool(),
                packet.Readint(),
                packet.Readint(),
                packet.Readint(),
                packet.Readfloat(),
                packet.Readbool(),
                packet.Readfloat(),
                packet.Readfloat(),
                packet.Readfloat(),
                packet.Readfloat()
            );
        }

        public override void Write(Packet packet)
        {
            packet.Write(rpm);
            packet.Write(isStable);
            packet.Write(state);
            packet.Write(health);
            packet.Write(chargeIndex);
            packet.Write(cooldownRemaining);
            packet.Write(isCharging);
            packet.Write(chargeProgress);
            packet.Write(wobble);
            packet.Write(targetMin);
            packet.Write(targetMax);
        }

        public void ClientHandle()
        {
            if (PeakOfEnergyManager.Instance == null)
            {
                Debug.LogWarning("[NMS_Server_PeakOfEnergyState] No PeakOfEnergyManager in scene to mirror state to.");
                return;
            }

            PeakOfEnergyManager.Instance.ApplyNetworkState(
                rpm, isStable, (PeakOfEnergyManager.MissionState)state, health, chargeIndex,
                cooldownRemaining, isCharging, chargeProgress, wobble, targetMin, targetMax);
        }
    }
}
