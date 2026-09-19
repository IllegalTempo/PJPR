using NUnit.Framework;
using UnityEngine;

public class MissionTravelStateTests
{
    [Test]
    public void Loading_RemovesDisconnectedPeerAndRejectsStaleAck()
    {
        var state = new MissionTravelState();
        Assert.That(state.TryBeginVote(), Is.True);
        state.SetOutboundPortal("Peak of Energy", "PeakOfEnergy", "portal-1", new Vector3(10f, 0f, 0f));
        Assert.That(state.TryBeginLoading(7, new ulong[] { 11, 22 }), Is.True);
        Assert.That(state.Acknowledge(11, 6), Is.False);
        Assert.That(state.Acknowledge(11, 7), Is.True);
        state.MarkHostLoaded(7);
        Assert.That(state.AllPeersLoaded, Is.False);
        state.RemoveExpectedPeer(22);
        Assert.That(state.AllPeersLoaded, Is.True);
    }

    [Test]
    public void Outcome_IsRecordedOnlyOnce()
    {
        MissionTravelState state = CreateActiveState();

        Assert.That(state.RecordOutcome(MissionOutcome.Succeeded), Is.True);
        Assert.That(state.RecordOutcome(MissionOutcome.Failed), Is.False);
        Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.Succeeded));
    }

    [Test]
    public void ApplySnapshot_IgnoresOlderSession()
    {
        MissionTravelState state = CreateActiveState(sessionId: 8);
        MissionTravelSnapshot older = new MissionTravelSnapshot(
            MissionTravelPhase.OutboundPortal,
            7,
            "Mission3",
            "Mission3",
            "old-portal",
            Vector3.zero,
            Quaternion.identity,
            MissionOutcome.None);

        Assert.That(state.ApplySnapshot(older), Is.False);
        Assert.That(state.SessionId, Is.EqualTo(8));
        Assert.That(state.Phase, Is.EqualTo(MissionTravelPhase.MissionActive));
    }

    private static MissionTravelState CreateActiveState(int sessionId = 1)
    {
        var state = new MissionTravelState();
        Assert.That(state.TryBeginVote(), Is.True);
        state.SetOutboundPortal("Peak of Energy", "PeakOfEnergy", "outbound", new Vector3(10f, 0f, 0f));
        Assert.That(state.TryBeginLoading(
            sessionId,
            System.Array.Empty<ulong>(),
            new Quaternion(0f, 0.2164396f, 0f, 0.976296f)), Is.True);
        Assert.That(state.MarkHostLoaded(sessionId), Is.True);
        Assert.That(state.EnterMission("return-portal"), Is.True);
        return state;
    }
}
