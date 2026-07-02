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

        public NMS_Server_NewObject(string prefabId, string uid, Vector3 spawnLocation, Quaternion spawnRotation, ulong owner) : base((int)packets.ServerPackets.NewObject)
        {
            this.prefabId = prefabId;
            this.uid = uid;
            this.spawnLocation = spawnLocation;
            this.spawnRotation = spawnRotation;
            this.owner = owner;
            Debug.Log("NEW SERVER NEW OBJECT MESSAGE GENERATED");
        }

        public static NMS_Server_NewObject Read(Packet packet)
        {
            return new NMS_Server_NewObject(packet.ReadstringUNICODE(), packet.ReadstringUNICODE(), packet.Readvector3(), packet.Readquaternion(), packet.Readulong());
        }

        public override void Write(Packet packet)
        {
            packet.WriteUNICODE(prefabId);
            packet.WriteUNICODE(uid);
            packet.Write(spawnLocation);
            packet.Write(spawnRotation);
            packet.Write(owner);
        }

        public void ClientHandle()
        {
            Debug.Log("Received Server New Object: " + prefabId);
            GameCore.Instance.spawnNetworkPrefab(prefabId, owner, uid, spawnLocation, spawnRotation).Forget();
        }
    }
}
