using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Both_PickUpItem : NMS_BOTH_SHARE
    {
        private const float MaxAcceptedThrowForce = 10f;
        private readonly string itemId;
        private readonly ulong pickedUpBy;
        private readonly Vector3 dropPosition;
        private readonly Quaternion dropRotation;
        private readonly Vector3 throwDirection;
        private readonly float throwForce;

        public NMS_Both_PickUpItem(string itemId, ulong pickedUpBy)
            : this(itemId, pickedUpBy, Vector3.zero, Quaternion.identity, Vector3.zero, 0f)
        {
        }

        public NMS_Both_PickUpItem(string itemId, ulong pickedUpBy, Vector3 dropPosition, Quaternion dropRotation, Vector3 throwDirection, float throwForce) : base((int)packets.BothPackets.PickUpItem)
        {
            this.itemId = itemId;
            this.pickedUpBy = pickedUpBy;
            this.dropPosition = dropPosition;
            this.dropRotation = dropRotation;
            this.throwDirection = throwDirection;
            this.throwForce = SanitizeThrowForce(throwForce);
        }

        private static float SanitizeThrowForce(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp(value, 0f, MaxAcceptedThrowForce);
        }

        public static NMS_Both_PickUpItem Read(Packet packet)
        {
            return new NMS_Both_PickUpItem(
                packet.ReadstringUNICODE(),
                packet.Readulong(),
                packet.Readvector3(),
                packet.Readquaternion(),
                packet.Readvector3(),
                packet.Readfloat());
        }

        public override void Write(Packet packet)
        {
            packet.Write(itemId);
            packet.Write(pickedUpBy);
            packet.Write(dropPosition);
            packet.Write(dropRotation);
            packet.Write(throwDirection);
            packet.Write(throwForce);
        }

        protected override void applyaction()
        {
            if(NetworkSystem.Instance.FindNetworkIdentity.ContainsKey(itemId))
            {
                NetworkSystem.Instance.GetComponentOfIdentity<Item>(itemId).Network_onPickUPorDrop(pickedUpBy, dropPosition, dropRotation, throwDirection, throwForce);

            } else
            {
                throw new NO_Not_Found(itemId);
            }

        }
        public override void ServerHandle(NetworkPlayer player)
        {
            if (!NetworkSystem.Instance.FindNetworkIdentity.TryGetValue(itemId, out NetworkIdentity networkObject))
            {
                throw new NO_Not_Found(itemId);
            }

            bool isDropRequest = pickedUpBy == 0;
            if (!isDropRequest && NetworkSystem.Instance.FindNetworkIdentity[itemId].Sovereignty != 0)
            {
                Debug.LogWarning("Rejected pickup");
                return;
            }
            if (isDropRequest)
            {
                if (((NetworkPrefabIdentity)networkObject).Sovereignty != player.steamId)
                {
                    Debug.LogWarning($"Rejected drop for {itemId}: {player.steamId} does not own it.");
                    return;
                }
            }
            else if (player.steamId != pickedUpBy)
            {
                
                    Debug.LogWarning($"Rejected pickup for {itemId}: sender {player.steamId} tried to set owner {pickedUpBy}.");
                return;
                
                
            }

            base.ServerHandle(player);
        }

    }
}
