using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SyncNetworkPrefab : NMS, IClientHandle
    {
        private readonly NetworkObjectSnapshot[] objects;
        private readonly SlotSnapshot[] slotsRelationships;

        public NMS_Server_SyncNetworkPrefab(IEnumerable<NetworkObjectSnapshot> objects, IEnumerable<SlotSnapshot> sr) : base((int)packets.ServerPackets.SyncNetworkObjects)
        {
            this.objects = new List<NetworkObjectSnapshot>(objects).ToArray();
            this.slotsRelationships = new List<SlotSnapshot>(sr).ToArray();
        }

        public NMS_Server_SyncNetworkPrefab(IEnumerable<NetworkPrefabIdentity> networkObjects, IEnumerable<Slot> slots) : base((int)packets.ServerPackets.SyncNetworkObjects)
        {
            List<NetworkObjectSnapshot> snapshots = new List<NetworkObjectSnapshot>();
            foreach (NetworkPrefabIdentity networkObject in networkObjects)
            {
                snapshots.Add(new NetworkObjectSnapshot(
                    networkObject.Identifier,
                    networkObject.Sovereignty,
                    networkObject.PrefabID,
                    networkObject.transform.position,
                    networkObject.transform.rotation));
            }
            List<SlotSnapshot> slotSnapshots = new List<SlotSnapshot>();
            foreach (Slot slot in slots)
            {
                string attachedItemId = slot.GetAttachedItem()?.GetNetworkObject()?.Identity?.Identifier ?? string.Empty;
                slotSnapshots.Add(new SlotSnapshot
                (
                    slot.Identity.Identifier,
                    attachedItemId
                ));
            }
            objects = snapshots.ToArray();
            slotsRelationships = slotSnapshots.ToArray();
        }

        public static NMS_Server_SyncNetworkPrefab Read(Packet packet)
        {
            int objectlistlength = packet.Readint();
            NetworkObjectSnapshot[] objects = new NetworkObjectSnapshot[objectlistlength];
            for (int i = 0; i < objectlistlength; i++)
            {
                objects[i] = new NetworkObjectSnapshot(
                    packet.ReadstringUNICODE(),
                    packet.Readulong(),
                    packet.ReadstringUNICODE(),
                    packet.Readvector3(),
                    packet.Readquaternion());
            }
            int slotlistlength = packet.Readint();
            SlotSnapshot[] slotsRelationships = new SlotSnapshot[slotlistlength];
            for(int i = 0; i < slotlistlength; i++)
            {
                slotsRelationships[i] = new SlotSnapshot(
                    packet.ReadstringUNICODE(),
                    packet.ReadstringUNICODE());
            }

            return new NMS_Server_SyncNetworkPrefab(objects,slotsRelationships);
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
            packet.Write(slotsRelationships.Length);
            foreach (SlotSnapshot snapshot in slotsRelationships)
            {
                packet.WriteUNICODE(snapshot.SlotId);
                packet.WriteUNICODE(snapshot.AttachedItemId);
            }

        }

        public void ClientHandle()
        {
            Debug.Log($"Syncing {objects.Length} Network Objects from Server");
            foreach (NetworkObjectSnapshot snapshot in objects)
            {
                GameCore.Instance.spawnNetworkPrefab(snapshot.PrefabId, snapshot.Owner, snapshot.Uid, snapshot.Position, snapshot.Rotation).Forget();
            }
            foreach (SlotSnapshot snapshot in slotsRelationships)
            {
                if(snapshot.AttachedItemId == string.Empty)
                {
                    Debug.Log("No item attached to slot: " + snapshot.SlotId);
                    continue;
                }
                Debug.Log("Syncing Slot Relationship: " + snapshot.SlotId + " -> " + snapshot.AttachedItemId);
                Slot slot = NetworkSystem.Instance.GetComponentOfIdentity<Slot>(snapshot.SlotId);
                Item attachedItem = NetworkSystem.Instance.GetComponentOfIdentity<Item>(snapshot.AttachedItemId);
                if (attachedItem != null&&slot != null && attachedItem != null)
                {
                    slot.Attach(attachedItem);
                } else
                {
                    Debug.LogError($"Failed to attach item {snapshot.AttachedItemId} to slot {snapshot.SlotId}. Slot or Item not found.");
                }
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
        public readonly struct SlotSnapshot
        {
            public readonly string SlotId;
            public readonly string AttachedItemId;
            public SlotSnapshot(string slotId, string attachedItemId)
            {
                SlotId = slotId;
                AttachedItemId = attachedItemId;
            }
        }

    }
}
