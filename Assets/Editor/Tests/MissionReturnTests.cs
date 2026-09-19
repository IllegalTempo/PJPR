using NUnit.Framework;
using UnityEngine;

public class MissionReturnTests
{
    private GameObject managerObject;

    [TearDown]
    public void TearDown()
    {
        if (managerObject != null)
            Object.DestroyImmediate(managerObject);
    }

    [Test]
    public void CompletedMission_RecordsSuccessOnlyOnce()
    {
        MissionManager manager = CreateActiveManager("Mission3");

        Assert.That(manager.ReportMissionEnded("Mission3", true), Is.True);
        Assert.That(manager.ReportMissionEnded("Mission3", false), Is.False);
        Assert.That(manager.TravelSnapshot.Outcome, Is.EqualTo(MissionOutcome.Succeeded));
    }

    [Test]
    public void EarlyReturnRequest_RecordsFailure()
    {
        MissionManager manager = CreateActiveManager("Mission4");

        Assert.That(manager.RequestActiveMissionFailure(), Is.True);
        Assert.That(manager.TravelSnapshot.Outcome, Is.EqualTo(MissionOutcome.Failed));
    }

    private MissionManager CreateActiveManager(string missionName)
    {
        managerObject = new GameObject("MissionReturnTests Manager");
        MissionManager manager = managerObject.AddComponent<MissionManager>();
        MissionTravelState state = manager.TestTravelState;
        Assert.That(state.TryBeginVote(), Is.True);
        state.SetOutboundPortal(missionName, missionName, "outbound", new Vector3(20f, 0f, 0f));
        Assert.That(state.TryBeginLoading(1, System.Array.Empty<ulong>()), Is.True);
        Assert.That(state.MarkHostLoaded(1), Is.True);
        Assert.That(state.EnterMission("return"), Is.True);
        return manager;
    }
}
