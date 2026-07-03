using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SyncScene : NMS, IClientHandle
    {
        private readonly NetworkObjectSnapshot[] objects;
        private readonly SlotSnapshot[] slotsRelationships;

        // Voting session data embedded for late-join sync
        private readonly bool hasVotingSession;
        private readonly Mission[] votingMissions;
        private readonly float votingTimerRemaining;
        private readonly int[] voteCounts;

        // Constructor used by Read() — receives all data from the packet
        public NMS_Server_SyncScene(
            IEnumerable<NetworkObjectSnapshot> objects,
            IEnumerable<SlotSnapshot> sr,
            bool hasVotingSession,
            Mission[] votingMissions,
            float votingTimerRemaining,
            int[] voteCounts) : base((int)packets.ServerPackets.SyncNetworkObjects)
        {
            this.objects = new List<NetworkObjectSnapshot>(objects).ToArray();
            this.slotsRelationships = new List<SlotSnapshot>(sr).ToArray();
            this.hasVotingSession = hasVotingSession;
            this.votingMissions = votingMissions;
            this.votingTimerRemaining = votingTimerRemaining;
            this.voteCounts = voteCounts;
        }

        public NMS_Server_SyncScene(IEnumerable<NetworkPrefabIdentity> networkObjects, IEnumerable<Slot> slots) : base((int)packets.ServerPackets.SyncNetworkObjects)
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
                    attachedItemId,
                    slot.GetAttachedItem()?.transform.rotation ?? Quaternion.identity
                ));
            }
            objects = snapshots.ToArray();
            slotsRelationships = slotSnapshots.ToArray();

            MissionManager mm = MissionManager.Instance;
            if (mm != null && mm.IsVotingActive && mm.CurrentVotingMissions != null)
            {
                hasVotingSession = true;
                votingMissions = mm.CurrentVotingMissions;
                votingTimerRemaining = mm.VotingTimer;
                voteCounts = mm.GetCurrentVoteCounts();
            }
            else
            {
                hasVotingSession = false;
                votingMissions = null;
                votingTimerRemaining = 0f;
                voteCounts = null;
            }
        }

        public static NMS_Server_SyncScene Read(Packet packet)
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
                    packet.ReadstringUNICODE(),
                    packet.Readquaternion()

                    );
            }

            bool hasVotingSession = packet.Readbool();
            if (hasVotingSession)
            {
                int missionCount = packet.Readint();
                Mission[] votingMissions = new Mission[missionCount];
                for (int i = 0; i < missionCount; i++)
                    votingMissions[i] = packet.ReadMission();

                float votingTimerRemaining = packet.Readfloat();
                int voteCountsLength = packet.Readint();
                int[] voteCounts = new int[voteCountsLength];
                for (int i = 0; i < voteCountsLength; i++)
                    voteCounts[i] = packet.Readint();

                return new NMS_Server_SyncScene(
                    objects, slotsRelationships,
                    true, votingMissions,
                    votingTimerRemaining, voteCounts);
            }

            return new NMS_Server_SyncScene(
                objects, slotsRelationships,
                false, null, 0f, null);
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
                packet.Write(snapshot.rotation);
            }

            packet.Write(hasVotingSession);
            if (hasVotingSession)
            {
                packet.Write(votingMissions.Length);
                foreach (Mission m in votingMissions)
                    packet.Write(m);

                packet.Write(votingTimerRemaining);
                packet.Write(voteCounts != null ? voteCounts.Length : 0);
                if (voteCounts != null)
                {
                    foreach (int count in voteCounts)
                        packet.Write(count);
                }
            }

        }

        public async void ClientHandle()
        {
            Debug.Log($"Syncing {objects.Length} Network Objects from Server");
            foreach (NetworkObjectSnapshot snapshot in objects)
            {
                await GameCore.Instance.spawnNetworkPrefab(snapshot.PrefabId, snapshot.Owner, snapshot.Uid, snapshot.Position, snapshot.Rotation);
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
                    slot.Attach(attachedItem,snapshot.rotation);
                } else
                {
                    Debug.LogError($"Failed to attach item {snapshot.AttachedItemId} to slot {snapshot.SlotId}. Slot or Item not found.");
                }
            }

            NetworkRouter.Instance.UpdateReadyState(ReadyState.SyncNetworkObjects);

            if (hasVotingSession && votingMissions != null && MissionProjectionDisplay.Instance != null)
            {
                MissionProjectionDisplay.Instance.ShowVotingMissions(votingMissions, votingTimerRemaining);

                if (voteCounts != null)
                    MissionProjectionDisplay.Instance.UpdateVoteCounts(voteCounts);

                Debug.Log($"[NMS_Server_SyncScene] Restored voting session: {votingMissions.Length} missions, {votingTimerRemaining:F1}s remaining.");
            }
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
            public readonly Quaternion rotation;
            public SlotSnapshot(string slotId, string attachedItemId, Quaternion rotation)
            {
                SlotId = slotId;
                AttachedItemId = attachedItemId;
                this.rotation = rotation;
            }
        }

    }
}
