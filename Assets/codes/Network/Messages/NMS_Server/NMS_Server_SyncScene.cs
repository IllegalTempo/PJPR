using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SyncScene : NMS, IClientHandle
    {
        private readonly NetworkObjectSnapshot[] sceneNetworkObjects;
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
            this.sceneNetworkObjects = new List<NetworkObjectSnapshot>(objects).ToArray();
            this.slotsRelationships = new List<SlotSnapshot>(sr).ToArray();
            this.hasVotingSession = hasVotingSession;
            this.votingMissions = votingMissions;
            this.votingTimerRemaining = votingTimerRemaining;
            this.voteCounts = voteCounts;
        }

        public NMS_Server_SyncScene( IEnumerable<Slot> slots) : base((int)packets.ServerPackets.SyncNetworkObjects)
        {
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
            sceneNetworkObjects = NetworkObjectSnapshot.GetNetworkPrefabSnapshotInScene().ToArray();
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
        private List<NetworkObjectSnapshot> GetSceneNetworkObjects()
        {
            return new List<NetworkObjectSnapshot>(sceneNetworkObjects);
        }
        public static NMS_Server_SyncScene Read(Packet packet)
        {
            NetworkObjectSnapshot[] objects = packet.ReadArray<NetworkObjectSnapshot>();
            SlotSnapshot[] slotsRelationships = packet.ReadArray<SlotSnapshot>();

            bool hasVotingSession = packet.Readbool();
            if (hasVotingSession)
            {
                Mission[] votingMissions = packet.ReadArray<Mission>();
                float votingTimerRemaining = packet.Readfloat();
                int[] voteCounts = packet.ReadArray<int>();

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
            packet.Write(sceneNetworkObjects);
            packet.Write(slotsRelationships);

            packet.Write(hasVotingSession);
            if (hasVotingSession)
            {
                packet.Write(votingMissions);
                packet.Write(votingTimerRemaining);
                packet.Write(voteCounts);
            }

        }

        public async void ClientHandle()
        {
            Debug.Log($"Syncing {sceneNetworkObjects.Length} Network Objects from Server");
            foreach (NetworkObjectSnapshot snapshot in sceneNetworkObjects)
            {
                await GameCore.Instance.spawnNetworkPrefab(snapshot.PrefabId, snapshot.Owner, snapshot.Uid, snapshot.Position, snapshot.Rotation);
            }
            GameInitManager.Instance.InitSlotRelationFromSave(slotsRelationships);

            NetworkRouter.Instance.UpdateReadyState(ReadyState.SyncNetworkObjects);

            if (hasVotingSession && votingMissions != null && MissionProjectionDisplay.Instance != null)
            {
                MissionProjectionDisplay.Instance.ShowVotingMissions(votingMissions, votingTimerRemaining);

                if (voteCounts != null)
                    MissionProjectionDisplay.Instance.UpdateVoteCounts(voteCounts);

                Debug.Log($"[NMS_Server_SyncScene] Restored voting session: {votingMissions.Length} missions, {votingTimerRemaining:F1}s remaining.");
            }
        }

        

    }
    
}
