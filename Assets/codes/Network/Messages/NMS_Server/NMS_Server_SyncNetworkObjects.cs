using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SyncNetworkObjects : NMS, IClientHandle
    {
        private readonly NetworkObjectSnapshot[] objects;

        public NMS_Server_SyncNetworkObjects(IEnumerable<NetworkObjectSnapshot> objects) : base((int)packets.ServerPackets.SyncNetworkObjects)
        {
            this.objects = new List<NetworkObjectSnapshot>(objects).ToArray();
        }

        public NMS_Server_SyncNetworkObjects(IEnumerable<NetworkObject> networkObjects) : base((int)packets.ServerPackets.SyncNetworkObjects)
        {
            List<NetworkObjectSnapshot> snapshots = new List<NetworkObjectSnapshot>();
            foreach (NetworkObject networkObject in networkObjects)
            {
                snapshots.Add(new NetworkObjectSnapshot(
                    networkObject.Identifier,
                    networkObject.Owner,
                    networkObject.PrefabID,
                    networkObject.transform.position,
                    networkObject.transform.rotation));
            }

            objects = snapshots.ToArray();
        }

        public static NMS_Server_SyncNetworkObjects Read(Packet packet)
        {
            int length = packet.Readint();
            NetworkObjectSnapshot[] objects = new NetworkObjectSnapshot[length];
            for (int i = 0; i < length; i++)
            {
                objects[i] = new NetworkObjectSnapshot(
                    packet.ReadstringUNICODE(),
                    packet.Readulong(),
                    packet.ReadstringUNICODE(),
                    packet.Readvector3(),
                    packet.Readquaternion());
            }

            return new NMS_Server_SyncNetworkObjects(objects);
        }

        public override void Write(Packet packet)
        {
            packet.Write(objects.Length);
            foreach (NetworkObjectSnapshot snapshot in objects)
            {
                packet.WriteUNICODE(snapshot.Uid);
                packet.Write(snapshot.Owner);
                packet.WriteUNICODE(snapshot.PrefabId);
                packet.Write(snapshot.Position);
                packet.Write(snapshot.Rotation);
            }
        }

        public void ClientHandle()
        {
            Debug.Log($"Syncing {objects.Length} Network Objects from Server");
            foreach (NetworkObjectSnapshot snapshot in objects)
            {
                GameCore.Instance.spawnNetworkPrefab(snapshot.PrefabId, snapshot.Owner, snapshot.Uid, snapshot.Position, snapshot.Rotation).Forget();
            }

            NetworkRouter.Instance.UpdateReadyState(ReadyState.SyncNetworkObjects);
        }

        public readonly struct NetworkObjectSnapshot
        {
            public readonly string Uid;
            public readonly ulong Owner;
            public readonly string PrefabId;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;

            public NetworkObjectSnapshot(string uid, ulong owner, string prefabId, Vector3 position, Quaternion rotation)
            {
                Uid = uid;
                Owner = owner;
                PrefabId = prefabId;
                Position = position;
                Rotation = rotation;
            }
        }
    }
}
