using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SyncScene : NMS, IClientHandle
    {
        private readonly NetworkObjectSnapshot[] objects;
        private readonly SlotSnapshot[] slotsRelationships;

        private readonly bool hasVotingSession;
        private readonly int votingMissionCount;
        private readonly string[] missionNames;
        private readonly string[] missionDescriptions;
        private readonly int[] rewardCredits;
        private readonly float[] difficulties;
        private readonly int[] durations;
        private readonly float votingTimerRemaining;
        private readonly int[] voteCounts;

        public NMS_Server_SyncScene(
            IEnumerable<NetworkObjectSnapshot> objects,
            IEnumerable<SlotSnapshot> sr,
            bool hasVotingSession,
            int votingMissionCount,
            string[] missionNames,
            string[] missionDescriptions,
            int[] rewardCredits,
            float[] difficulties,
            int[] durations,
            float votingTimerRemaining,
            int[] voteCounts) : base((int)packets.ServerPackets.SyncNetworkObjects)
        {
            this.objects = new List<NetworkObjectSnapshot>(objects).ToArray();
            this.slotsRelationships = new List<SlotSnapshot>(sr).ToArray();
            this.hasVotingSession = hasVotingSession;
            this.votingMissionCount = votingMissionCount;
            this.missionNames = missionNames;
            this.missionDescriptions = missionDescriptions;
            this.rewardCredits = rewardCredits;
            this.difficulties = difficulties;
            this.durations = durations;
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
                Mission[] missions = mm.CurrentVotingMissions;
                votingMissionCount = missions.Length;
                missionNames = new string[votingMissionCount];
                missionDescriptions = new string[votingMissionCount];
                rewardCredits = new int[votingMissionCount];
                difficulties = new float[votingMissionCount];
                durations = new int[votingMissionCount];
                for (int i = 0; i < votingMissionCount; i++)
                {
                    missionNames[i] = missions[i].missionName;
                    missionDescriptions[i] = missions[i].missionDescription;
                    rewardCredits[i] = missions[i].rewardCredits;
                    difficulties[i] = missions[i].difficulty;
                    durations[i] = missions[i].estimatedDuration;
                }
                votingTimerRemaining = mm.VotingTimer;
                voteCounts = mm.GetCurrentVoteCounts();
            }
            else
            {
                hasVotingSession = false;
                votingMissionCount = 0;
                missionNames = null;
                missionDescriptions = null;
                rewardCredits = null;
                difficulties = null;
                durations = null;
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
                string[] missionNames = new string[missionCount];
                string[] missionDescriptions = new string[missionCount];
                int[] rewardCredits = new int[missionCount];
                float[] difficulties = new float[missionCount];
                int[] durations = new int[missionCount];
                for (int i = 0; i < missionCount; i++)
                {
                    missionNames[i] = packet.ReadstringUNICODE();
                    missionDescriptions[i] = packet.ReadstringUNICODE();
                    rewardCredits[i] = packet.Readint();
                    difficulties[i] = packet.Readfloat();
                    durations[i] = packet.Readint();
                }
                float votingTimerRemaining = packet.Readfloat();
                int voteCountsLength = packet.Readint();
                int[] voteCounts = new int[voteCountsLength];
                for (int i = 0; i < voteCountsLength; i++)
                    voteCounts[i] = packet.Readint();

                return new NMS_Server_SyncScene(
                    objects, slotsRelationships,
                    true, missionCount,
                    missionNames, missionDescriptions, rewardCredits,
                    difficulties, durations,
                    votingTimerRemaining, voteCounts);
            }

            return new NMS_Server_SyncScene(
                objects, slotsRelationships,
                false, 0, null, null, null, null, null, 0f, null);
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
                packet.Write(votingMissionCount);
                for (int i = 0; i < votingMissionCount; i++)
                {
                    packet.WriteUNICODE(missionNames[i]);
                    packet.WriteUNICODE(missionDescriptions[i]);
                    packet.Write(rewardCredits[i]);
                    packet.Write(difficulties[i]);
                    packet.Write(durations[i]);
                }
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
            
            if (hasVotingSession && MissionProjectionDisplay.Instance != null)
            {
                Mission[] missions = new Mission[votingMissionCount];
                for (int i = 0; i < votingMissionCount; i++)
                {
                    missions[i] = new Mission(
                        missionNames[i],
                        missionDescriptions[i],
                        rewardCredits[i],
                        difficulties[i] * 10f,
                        durations[i]
                    );
                }

                MissionProjectionDisplay.Instance.ShowVotingMissions(missions, votingTimerRemaining);

                if (voteCounts != null)
                    MissionProjectionDisplay.Instance.UpdateVoteCounts(voteCounts);

                Debug.Log($"[NMS_Server_SyncScene] Restored voting session: {votingMissionCount} missions, {votingTimerRemaining:F1}s remaining.");
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
