using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionTravelPhase
{
    Idle,
    Voting,
    OutboundPortal,
    LoadingMission,
    MissionActive,
    Returning
}

public enum MissionOutcome
{
    None,
    Succeeded,
    Failed
}

[Serializable]
public readonly struct MissionTravelSnapshot : IEquatable<MissionTravelSnapshot>
{
    public MissionTravelPhase Phase { get; }
    public int SessionId { get; }
    public string MissionName { get; }
    public string SceneName { get; }
    public string ActivePortalId { get; }
    public Vector3 ReturnPosition { get; }
    public Quaternion ReturnRotation { get; }
    public MissionOutcome Outcome { get; }

    public MissionTravelSnapshot(
        MissionTravelPhase phase,
        int sessionId,
        string missionName,
        string sceneName,
        string activePortalId,
        Vector3 returnPosition,
        Quaternion returnRotation,
        MissionOutcome outcome)
    {
        Phase = phase;
        SessionId = sessionId;
        MissionName = missionName ?? string.Empty;
        SceneName = sceneName ?? string.Empty;
        ActivePortalId = activePortalId ?? string.Empty;
        ReturnPosition = returnPosition;
        ReturnRotation = returnRotation;
        Outcome = outcome;
    }

    public bool Equals(MissionTravelSnapshot other)
    {
        return Phase == other.Phase &&
               SessionId == other.SessionId &&
               MissionName == other.MissionName &&
               SceneName == other.SceneName &&
               ActivePortalId == other.ActivePortalId &&
               ReturnPosition == other.ReturnPosition &&
               ReturnRotation == other.ReturnRotation &&
               Outcome == other.Outcome;
    }

    public override bool Equals(object obj)
    {
        return obj is MissionTravelSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            (int)Phase,
            SessionId,
            MissionName,
            SceneName,
            ActivePortalId,
            ReturnPosition,
            ReturnRotation,
            (int)Outcome);
    }
}

public sealed class MissionTravelState
{
    private readonly HashSet<ulong> expectedPeers = new();
    private readonly HashSet<ulong> readyPeers = new();
    private bool hostLoaded;

    public MissionTravelPhase Phase { get; private set; } = MissionTravelPhase.Idle;
    public int SessionId { get; private set; }
    public string MissionName { get; private set; } = string.Empty;
    public string SceneName { get; private set; } = string.Empty;
    public string ActivePortalId { get; private set; } = string.Empty;
    public Vector3 ReturnPosition { get; private set; }
    public Quaternion ReturnRotation { get; private set; } = Quaternion.identity;
    public MissionOutcome Outcome { get; private set; }

    public bool AllPeersLoaded =>
        Phase == MissionTravelPhase.LoadingMission &&
        hostLoaded &&
        expectedPeers.IsSubsetOf(readyPeers);

    public MissionTravelSnapshot Snapshot => new(
        Phase,
        SessionId,
        MissionName,
        SceneName,
        ActivePortalId,
        ReturnPosition,
        ReturnRotation,
        Outcome);

    public bool TryBeginVote()
    {
        if (Phase != MissionTravelPhase.Idle)
            return false;

        Phase = MissionTravelPhase.Voting;
        return true;
    }

    public void SetOutboundPortal(
        string missionName,
        string sceneName,
        string portalId,
        Vector3 returnPosition)
    {
        if (Phase != MissionTravelPhase.Voting)
            throw new InvalidOperationException($"Cannot create an outbound portal while travel is {Phase}.");
        if (string.IsNullOrWhiteSpace(missionName))
            throw new ArgumentException("Mission name is required.", nameof(missionName));
        if (string.IsNullOrWhiteSpace(sceneName))
            throw new ArgumentException("Mission scene name is required.", nameof(sceneName));
        if (string.IsNullOrWhiteSpace(portalId))
            throw new ArgumentException("Portal network ID is required.", nameof(portalId));

        MissionName = missionName;
        SceneName = sceneName;
        ActivePortalId = portalId;
        ReturnPosition = returnPosition;
        ReturnRotation = Quaternion.identity;
        Outcome = MissionOutcome.None;
        Phase = MissionTravelPhase.OutboundPortal;
    }

    public bool TryBeginLoading(
        int sessionId,
        IEnumerable<ulong> readyClientIds,
        Quaternion? returnRotation = null)
    {
        if (Phase != MissionTravelPhase.OutboundPortal || sessionId <= SessionId)
            return false;

        SessionId = sessionId;
        ReturnRotation = returnRotation ?? Quaternion.identity;
        expectedPeers.Clear();
        readyPeers.Clear();
        hostLoaded = false;

        if (readyClientIds != null)
        {
            foreach (ulong steamId in readyClientIds)
                expectedPeers.Add(steamId);
        }

        Phase = MissionTravelPhase.LoadingMission;
        return true;
    }

    public bool MarkHostLoaded(int sessionId)
    {
        if (Phase != MissionTravelPhase.LoadingMission || sessionId != SessionId || hostLoaded)
            return false;

        hostLoaded = true;
        return true;
    }

    public bool Acknowledge(ulong steamId, int sessionId)
    {
        if (Phase != MissionTravelPhase.LoadingMission || sessionId != SessionId)
            return false;
        if (!expectedPeers.Contains(steamId))
            return false;

        return readyPeers.Add(steamId);
    }

    public bool RemoveExpectedPeer(ulong steamId)
    {
        readyPeers.Remove(steamId);
        return expectedPeers.Remove(steamId);
    }

    public bool EnterMission(string returnPortalId)
    {
        if (!AllPeersLoaded || string.IsNullOrWhiteSpace(returnPortalId))
            return false;

        ActivePortalId = returnPortalId;
        Phase = MissionTravelPhase.MissionActive;
        return true;
    }

    public bool RecordOutcome(MissionOutcome outcome)
    {
        if ((Phase != MissionTravelPhase.MissionActive && Phase != MissionTravelPhase.Returning) ||
            Outcome != MissionOutcome.None ||
            outcome == MissionOutcome.None)
        {
            return false;
        }

        Outcome = outcome;
        return true;
    }

    public bool BeginReturning()
    {
        if (Phase != MissionTravelPhase.MissionActive)
            return false;

        Phase = MissionTravelPhase.Returning;
        return true;
    }

    public bool ApplySnapshot(MissionTravelSnapshot snapshot)
    {
        if (snapshot.SessionId < SessionId)
            return false;
        if (Snapshot.Equals(snapshot))
            return true;

        Phase = snapshot.Phase;
        SessionId = snapshot.SessionId;
        MissionName = snapshot.MissionName ?? string.Empty;
        SceneName = snapshot.SceneName ?? string.Empty;
        ActivePortalId = snapshot.ActivePortalId ?? string.Empty;
        ReturnPosition = snapshot.ReturnPosition;
        ReturnRotation = snapshot.ReturnRotation;
        Outcome = snapshot.Outcome;
        expectedPeers.Clear();
        readyPeers.Clear();
        hostLoaded = false;
        return true;
    }

    public void Reset()
    {
        Phase = MissionTravelPhase.Idle;
        SessionId = 0;
        MissionName = string.Empty;
        SceneName = string.Empty;
        ActivePortalId = string.Empty;
        ReturnPosition = Vector3.zero;
        ReturnRotation = Quaternion.identity;
        Outcome = MissionOutcome.None;
        expectedPeers.Clear();
        readyPeers.Clear();
        hostLoaded = false;
    }
}
