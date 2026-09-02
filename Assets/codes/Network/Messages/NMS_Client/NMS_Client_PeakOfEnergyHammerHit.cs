using UnityEngine;
using Assets.codes.Network.Messages;
using Assets.codes.Network.SyncedIdentity;

namespace Assets.codes.Network.Messages
{
    /// <summary>
    /// Client -> Server. A player hit an enemy with the Hammer (EVA melee).
    /// The server resolves the hit authoritatively (destroys Rust Eaters / mission meteorites).
    /// </summary>
    public class NMS_Client_PeakOfEnergyHammerHit : NMS, IServerHandle
    {
        private readonly string networkId;

        public NMS_Client_PeakOfEnergyHammerHit(string networkId) : base((int)packets.ClientPackets.PeakOfEnergyHammerHit)
        {
            this.networkId = networkId;
        }

        public static NMS_Client_PeakOfEnergyHammerHit Read(Packet packet)
        {
            return new NMS_Client_PeakOfEnergyHammerHit(packet.ReadstringUNICODE());
        }

        public override void Write(Packet packet)
        {
            packet.Write(networkId);
        }

        public void ServerHandle(NetworkPlayer player)
        {
            if (string.IsNullOrEmpty(networkId) || NetworkSystem.Instance == null)
                return;

            if (!NetworkSystem.Instance.FindNetworkIdentity.TryGetValue(networkId, out NetworkIdentity identity) || identity == null)
            {
                Debug.LogWarning($"[NMS_Client_PeakOfEnergyHammerHit] No network object with id '{networkId}'.");
                return;
            }

            if (identity.TryGetComponent(out RustEater rustEater))
            {
                rustEater.DestroyWithHammer();
                return;
            }

            if (identity.TryGetComponent(out PeakOfEnergyMeteorite meteorite))
            {
                meteorite.DestroyFromHammer();
            }
        }
    }
}
