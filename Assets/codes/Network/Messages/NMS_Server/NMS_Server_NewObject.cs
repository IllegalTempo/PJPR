using Assets.codes.items;
using Assets.codes.Network.SyncedIdentity;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_NewObject : NMS, IClientHandle
    {
        private readonly string prefabId;
        private readonly string uid;
        private readonly Vector3 spawnLocation;
        private readonly Quaternion spawnRotation;
        private readonly ulong owner;

        private readonly bool isCombining;
        private readonly string replacingitem1id;
        private readonly string replacingitem2id;
        public NMS_Server_NewObject(string prefabId, string uid, Vector3 spawnLocation, Quaternion spawnRotation, ulong owner, bool isCombining, string replacingitem1id, string replacingitem2id) : base((int)packets.ServerPackets.NewObject)
        {
            this.prefabId = prefabId;
            this.uid = uid;
            this.spawnLocation = spawnLocation;
            this.spawnRotation = spawnRotation;
            this.owner = owner;
            this.isCombining = isCombining;
            this.replacingitem1id = replacingitem1id;
            this.replacingitem2id = replacingitem2id;
            Debug.Log("NEW SERVER NEW OBJECT MESSAGE GENERATED");
        }
        public NMS_Server_NewObject(string prefabId, string uid, Vector3 spawnLocation, Quaternion spawnRotation, ulong owner, bool isCombining) : base((int)packets.ServerPackets.NewObject)
        {
            this.prefabId = prefabId;
            this.uid = uid;
            this.spawnLocation = spawnLocation;
            this.spawnRotation = spawnRotation;
            this.owner = owner;
            this.isCombining = isCombining;
            this.replacingitem1id = "";
            this.replacingitem2id = "";
            Debug.Log("NEW SERVER NEW OBJECT MESSAGE GENERATED");
        }
        public static NMS_Server_NewObject Read(Packet packet)
        {
            string prefabId = packet.ReadstringUNICODE();
            string uid = packet.ReadstringUNICODE();
            Vector3 spawnLocation = packet.Readvector3();
            Quaternion spawnRotation = packet.Readquaternion();
            ulong owner = packet.Readulong();
            bool isCombining = packet.Readbool();
            string replacingitem1id = packet.ReadstringUNICODE();
            string replacingitem2id = packet.ReadstringUNICODE();

            return new NMS_Server_NewObject(prefabId, uid, spawnLocation, spawnRotation, owner, isCombining, replacingitem1id, replacingitem2id);
        }

        public override void Write(Packet packet)
        {
            packet.Write(prefabId);
            packet.Write(uid);
            packet.Write(spawnLocation);
            packet.Write(spawnRotation);
            packet.Write(owner);
            packet.Write(isCombining);
            packet.Write(replacingitem1id);
            packet.Write(replacingitem2id);
        }

        public async UniTask ClientHandle()
        {
            NetworkGameObject nobj = await GameCore.Instance.spawnNetworkPrefab(prefabId, owner, uid, spawnLocation, spawnRotation);
            CombinedProcessableItem combinedProcessableItem = nobj.GetComponent<CombinedProcessableItem>();
            if (isCombining)
            {
                combinedProcessableItem.CombineIntoThis(NetworkSystem.Instance.GetComponentOfIdentity<Item>(replacingitem1id));
                combinedProcessableItem.CombineIntoThis(NetworkSystem.Instance.GetComponentOfIdentity<Item>(replacingitem2id));

            }
            Debug.Log("Received Server New Object: " + prefabId);
        }
    }
}
