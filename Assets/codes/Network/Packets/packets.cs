
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
        StartGameLoop = 1009,
        NO_Destroy = 1011,
        StartVotingSession = 1012,
        VoteResult = 1013,
        VoteUpdate = 1014,
        UpdateWorld_Velocity = 1015,
        UpdateWorld_Rotation = 1016,};
    public enum ClientPackets
    {
        SendReadyState = 2001,
        RequestVotingSession = 2004,
        CastVote = 2005,
    };
    public enum BothPackets
    {
        Test = 3001,
        PosUpdate = 3002,
        PlayerAnimation = 3003,
        NO_Info = 3005,
        NO_Active = 3006,
        PickUpItem = 3007,
        VoicePacket = 3008,
        NO_Slot_Interact = 3011,
        SlotDetach = 3012,
        QuantityResourceProviderInteract = 3013,
        SendCombineItem = 3014,
        Handle_OnReleaseUpdateLevel = 3015,};
}

