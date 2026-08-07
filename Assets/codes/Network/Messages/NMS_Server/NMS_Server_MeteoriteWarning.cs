using Assets.codes.Network;
using Assets.codes.Network.SyncedIdentity;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
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
            PrefabDefinition warningDef = NetworkSystem.Instance.GetPrefabDefinition("Warning");
            if (warningDef == null)
            {
                Debug.LogWarning("[NMS_Server_MeteoriteWarning] No PrefabDefinition found for 'Warning'.");
                return;
            }

            GameObject warningObj = null;

            if (warningDef.IsPoolPrefab)
            {
                NetworkPrefabPool pool = NetworkSystem.Instance.CurrentNetworkInstance.GetPool(warningDef);
                if (pool != null)
                {
                    NetworkGameObject nobj = pool.InstantiatePoolNetworkPrefab(
                        System.Guid.NewGuid().ToString(), Vector3.zero, Quaternion.identity);
                    if (nobj != null)
                        warningObj = nobj.gameObject;
                }
            }

            if (warningObj == null && warningDef.itemPrefab != null)
            {
                warningObj = GameObject.Instantiate(warningDef.itemPrefab, Vector3.zero, Quaternion.identity);
            }

            if (warningObj != null)
            {
                MeteoriteWarningIndicator indicator = warningObj.GetComponent<MeteoriteWarningIndicator>();
                if (indicator != null)
                {
                    indicator.Show(direction, duration);
                    indicator.PrefabDef = warningDef;
                }
                else
                {
                    if (warningDef.IsPoolPrefab)
                    {
                        NetworkSystem.Instance.CurrentNetworkInstance.GetPool(warningDef)
                            .Return(warningObj);
                    }
                    else
                    {
                        GameObject.Destroy(warningObj);
                    }
                }
            }
        }
    }
}
