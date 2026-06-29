
public class packets
{
    
    public enum ServerPackets
    {
        Test_Packet = 1001,
        RoomInfoOnPlayerEnterRoom = 1002,
        UpdatePlayerEnterRoomForExistingPlayer = 1003,
        PlayerQuit = 1004,
        NewObject = 1005,
        SyncNetworkObjects = 1006,
        DistributeVoicePacket = 1007,
        SendMissionInfo = 1008,
        StartGameLoop = 1009,
        SpawnMissionProjections = 1010,
    
        NO_Destroy = 1011,};
    public enum ClientPackets
    {
        SendReadyState = 2001,
        RequestMissions = 2002,
        AcceptMission = 2003,
    };
    public enum BothPackets
    {
        Test = 3001,
        PosUpdate = 3002,
        PlayerAnimation = 3003,
        Interact = 3004,
        NO_Info = 3005,
        NO_Active = 3006,
        PickUpItem = 3007,
        VoicePacket = 3008,
        NO_Slot_Interact = 3011,
        SlotDetach = 3012,};
}

