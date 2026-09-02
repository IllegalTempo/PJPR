using UnityEngine;
using Assets.codes.Network.Messages;

namespace Assets.codes.Network.Messages
{
    /// <summary>
    /// Client -> Server. Player pressed (true) or released (false) the Center charge button.
    /// The server validates RPM/stability and drives the actual channel timer.
    /// </summary>
    public class NMS_Client_PeakOfEnergyChargeRequest : NMS, IServerHandle
    {
        private readonly bool pressed;

        public NMS_Client_PeakOfEnergyChargeRequest(bool pressed) : base((int)packets.ClientPackets.PeakOfEnergyChargeRequest)
        {
            this.pressed = pressed;
        }

        public static NMS_Client_PeakOfEnergyChargeRequest Read(Packet packet)
        {
            return new NMS_Client_PeakOfEnergyChargeRequest(packet.Readbool());
        }

        public override void Write(Packet packet)
        {
            packet.Write(pressed);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            if (PeakOfEnergyManager.Instance == null)
            {
                Debug.LogWarning("[NMS_Client_PeakOfEnergyChargeRequest] No PeakOfEnergyManager in scene.");
                return;
            }

            if (pressed)
                PeakOfEnergyManager.Instance.RequestCharge();
            else
                PeakOfEnergyManager.Instance.ReleaseCharge();
        }
    }
}
