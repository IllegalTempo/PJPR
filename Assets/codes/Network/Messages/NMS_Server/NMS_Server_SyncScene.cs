using Cysharp.Threading.Tasks;
using Assets.codes.machines;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NMS_Server_SyncScene : NMS, IClientHandle
    {
        private readonly NetworkObjectSnapshot[] sceneNetworkObjects;
        private readonly SlotSnapshot[] slotsRelationships;
        private readonly int spaceshipIndex;

        // Voting session data embedded for late-join sync
        private readonly bool hasVotingSession;
        private readonly Mission[] votingMissions;
        private readonly float votingTimerRemaining;
        private readonly int[] voteCounts;
        private readonly int votingPlayerCount;
        public MissionTravelSnapshot TravelSnapshot { get; }
        public int SpaceshipSpeedStep { get; }

        // Constructor used by Read() — receives all data from the packet
        public NMS_Server_SyncScene(
            IEnumerable<NetworkObjectSnapshot> objects,
            IEnumerable<SlotSnapshot> sr,
            int spaceshipIndex,
            bool hasVotingSession,
            Mission[] votingMissions,
            float votingTimerRemaining,
            int[] voteCounts,
            int votingPlayerCount,
            MissionTravelSnapshot? travelSnapshot = null,
            int spaceshipSpeedStep = 0) : base((int)packets.ServerPackets.SyncNetworkObjects)
        {
            this.sceneNetworkObjects = new List<NetworkObjectSnapshot>(objects).ToArray();
            this.slotsRelationships = new List<SlotSnapshot>(sr).ToArray();
            this.spaceshipIndex = spaceshipIndex;
            this.hasVotingSession = hasVotingSession;
            this.votingMissions = votingMissions;
            this.votingTimerRemaining = votingTimerRemaining;
            this.voteCounts = voteCounts;
            this.votingPlayerCount = votingPlayerCount;
            TravelSnapshot = travelSnapshot ?? IdleTravelSnapshot();
            SpaceshipSpeedStep = spaceshipSpeedStep;
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
            spaceshipIndex = GameCore.Instance != null ? GameCore.Instance.CurrentSpaceshipIndex : 0;

            MissionManager mm = MissionManager.Instance;
            if (mm != null && mm.IsVotingActive && mm.CurrentVotingMissions != null)
            {
                hasVotingSession = true;
                votingMissions = mm.CurrentVotingMissions;
                votingTimerRemaining = mm.VotingTimer;
                voteCounts = mm.GetCurrentVoteCounts();
                votingPlayerCount = mm.GetVotingPlayerCount();
            }
            else
            {
                hasVotingSession = false;
                votingMissions = null;
                votingTimerRemaining = 0f;
                voteCounts = null;
                votingPlayerCount = 0;
            }

            mm?.RegisterLoadingPeersForSnapshot();
            TravelSnapshot = mm != null ? mm.TravelSnapshot : IdleTravelSnapshot();
            HandleControl speedHandle = MainSpaceship.Instance != null
                ? MainSpaceship.Instance.GetComponentInChildren<HandleControl>(true)
                : null;
            SpaceshipSpeedStep = speedHandle != null ? speedHandle.CurrentStep : 0;
        }
        private List<NetworkObjectSnapshot> GetSceneNetworkObjects()
        {
            return new List<NetworkObjectSnapshot>(sceneNetworkObjects);
        }
        public static NMS_Server_SyncScene Read(Packet packet)
        {
            NetworkObjectSnapshot[] objects = packet.ReadArray<NetworkObjectSnapshot>();
            SlotSnapshot[] slotsRelationships = packet.ReadArray<SlotSnapshot>();
            int spaceshipIndex = packet.Readint();

            bool hasVotingSession = packet.Readbool();
            Mission[] votingMissions = null;
            float votingTimerRemaining = 0f;
            int[] voteCounts = null;
            int votingPlayerCount = 0;
            if (hasVotingSession)
            {
                votingMissions = packet.ReadArray<Mission>();
                votingTimerRemaining = packet.Readfloat();
                voteCounts = packet.ReadArray<int>();
                votingPlayerCount = packet.Readint();
            }

            MissionTravelSnapshot travelSnapshot = ReadTravelSnapshot(packet);
            int spaceshipSpeedStep = packet.Readint();
            return new NMS_Server_SyncScene(
                objects, slotsRelationships,
                spaceshipIndex,
                hasVotingSession, votingMissions, votingTimerRemaining, voteCounts, votingPlayerCount,
                travelSnapshot, spaceshipSpeedStep);
        }

        public override void Write(Packet packet)
        {
            packet.Write(sceneNetworkObjects);
            packet.Write(slotsRelationships);
            packet.Write(spaceshipIndex);

            packet.Write(hasVotingSession);
            if (hasVotingSession)
            {
                packet.Write(votingMissions);
                packet.Write(votingTimerRemaining);
                packet.Write(voteCounts);
                packet.Write(votingPlayerCount);
            }

            WriteTravelSnapshot(packet, TravelSnapshot);
            packet.Write(SpaceshipSpeedStep);

        }

        public async void ClientHandle()
        {
            Debug.Log($"Syncing {sceneNetworkObjects.Length} Network Objects from Server");
            await GameCore.Instance.SpawnSpaceshipAsync(spaceshipIndex);
            MainSpaceship.Instance?.GetComponentInChildren<HandleControl>(true)?.OnStepChanged(SpaceshipSpeedStep);
            foreach (NetworkObjectSnapshot snapshot in sceneNetworkObjects)
            {
                await GameCore.Instance.spawnNetworkPrefab(snapshot.PrefabId, snapshot.Owner, snapshot.Uid, snapshot.Position, snapshot.Rotation);
            }
            GameInitManager.Instance.InitSlotRelationFromSave(slotsRelationships, false);

            if (hasVotingSession && votingMissions != null && MissionProjectionDisplay.Instance != null)
            {
                MissionProjectionDisplay.Instance.ShowVotingMissions(votingMissions, votingTimerRemaining, votingPlayerCount);

                if (voteCounts != null)
                    MissionProjectionDisplay.Instance.UpdateVoteCounts(voteCounts, votingPlayerCount);

                Debug.Log($"[NMS_Server_SyncScene] Restored voting session: {votingMissions.Length} missions, {votingTimerRemaining:F1}s remaining.");
            }

            if (MissionManager.Instance != null &&
                !await MissionManager.Instance.ApplyLateJoinTravelSnapshotAsync(TravelSnapshot))
            {
                Debug.LogError("[NMS_Server_SyncScene] Mission travel snapshot could not be applied; client remains unready.");
                return;
            }

            NetworkRouter.Instance.UpdateReadyState(ReadyState.SyncNetworkObjects);
            MissionManager.Instance?.NotifyLateJoinTravelReady(TravelSnapshot);
        }

        private static void WriteTravelSnapshot(Packet packet, MissionTravelSnapshot snapshot)
        {
            packet.Write((int)snapshot.Phase);
            packet.Write(snapshot.SessionId);
            packet.Write(snapshot.MissionName);
            packet.Write(snapshot.SceneName);
            packet.Write(snapshot.ActivePortalId);
            packet.Write(snapshot.ReturnPosition);
            packet.Write(snapshot.ReturnRotation);
            packet.Write((int)snapshot.Outcome);
        }

        private static MissionTravelSnapshot ReadTravelSnapshot(Packet packet)
        {
            return new MissionTravelSnapshot(
                (MissionTravelPhase)packet.Readint(),
                packet.Readint(),
                packet.ReadstringUNICODE(),
                packet.ReadstringUNICODE(),
                packet.ReadstringUNICODE(),
                packet.Readvector3(),
                packet.Readquaternion(),
                (MissionOutcome)packet.Readint());
        }

        private static MissionTravelSnapshot IdleTravelSnapshot()
        {
            return new MissionTravelSnapshot(
                MissionTravelPhase.Idle, 0, string.Empty, string.Empty, string.Empty,
                Vector3.zero, Quaternion.identity, MissionOutcome.None);
        }

        

    }
    
}
