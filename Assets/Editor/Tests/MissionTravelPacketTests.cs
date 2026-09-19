using Assets.codes.Network.Messages;
using NUnit.Framework;
using UnityEngine;

public class MissionTravelPacketTests
{
    [Test]
    public void LoadMissionScene_RoundTripsUnicodeAndSession()
    {
        NMS_Server_LoadMissionScene received = RoundTrip(
            new NMS_Server_LoadMissionScene(42, "Peak of 能量", "PeakOfEnergy"),
            NMS_Server_LoadMissionScene.Read);

        Assert.That(received.SessionId, Is.EqualTo(42));
        Assert.That(received.MissionName, Is.EqualTo("Peak of 能量"));
        Assert.That(received.SceneName, Is.EqualTo("PeakOfEnergy"));
    }

    [Test]
    public void ReadyEnterReturnAndAbort_RoundTripExactly()
    {
        NMS_Client_MissionSceneReady ready = RoundTrip(
            new NMS_Client_MissionSceneReady(3, "Mission3"), NMS_Client_MissionSceneReady.Read);
        Assert.That(ready.SessionId, Is.EqualTo(3));
        Assert.That(ready.SceneName, Is.EqualTo("Mission3"));
        Assert.That(ready.RequiresEntryCatchup, Is.True);

        NMS_Server_EnterMission enter = RoundTrip(
            new NMS_Server_EnterMission(3, Vector3.one, Quaternion.Euler(0f, 30f, 0f), "return-id"),
            NMS_Server_EnterMission.Read);
        Assert.That(enter.ReturnPortalId, Is.EqualTo("return-id"));

        NMS_Server_ReturnFromMission returned = RoundTrip(
            new NMS_Server_ReturnFromMission(3, Vector3.right, Quaternion.identity, true),
            NMS_Server_ReturnFromMission.Read);
        Assert.That(returned.Succeeded, Is.True);

        NMS_Server_AbortMissionLoad aborted = RoundTrip(
            new NMS_Server_AbortMissionLoad(3, "Mission3", string.Empty),
            NMS_Server_AbortMissionLoad.Read);
        Assert.That(aborted.Reason, Is.Empty);
    }

    private static T RoundTrip<T>(T sent, System.Func<Packet, T> read) where T : NMS
    {
        using var writer = new Packet(sent.PacketID);
        sent.Write(writer);
        using var reader = new Packet(writer.GetPacketData(9), null);
        T received = read(reader);
        Assert.That(reader.BytesRemaining, Is.Zero);
        return received;
    }
}
