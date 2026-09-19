using Assets.codes.Network.Messages;
using NUnit.Framework;
using UnityEngine;

public class MissionLateJoinTests
{
    [TestCase(MissionTravelPhase.OutboundPortal)]
    [TestCase(MissionTravelPhase.LoadingMission)]
    [TestCase(MissionTravelPhase.MissionActive)]
    [TestCase(MissionTravelPhase.Returning)]
    public void SyncScene_RoundTripsTravelSnapshot(MissionTravelPhase phase)
    {
        var expected = new MissionTravelSnapshot(
            phase, 17, "Peak of 能量", "PeakOfEnergy", "portal-17",
            new Vector3(4f, 5f, 6f), Quaternion.Euler(0f, 33f, 0f), MissionOutcome.Succeeded);
        var sent = new NMS_Server_SyncScene(
            System.Array.Empty<NetworkObjectSnapshot>(),
            System.Array.Empty<SlotSnapshot>(),
            2, false, null, 0f, null, 0, expected);

        using var writer = new Packet(sent.PacketID);
        sent.Write(writer);
        using var reader = new Packet(writer.GetPacketData(11), null);
        NMS_Server_SyncScene received = NMS_Server_SyncScene.Read(reader);

        Assert.That(received.TravelSnapshot, Is.EqualTo(expected));
        Assert.That(reader.BytesRemaining, Is.Zero);
    }
}
