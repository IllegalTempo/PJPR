using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

public class packets
{
    
    public enum ServerPackets
    {
        Test_Packet = 0,
        RoomInfoOnPlayerEnterRoom = 1,
        UpdatePlayerEnterRoomForExistingPlayer = 2,
        PlayerQuit = 3,
        DistributeMovement = 4,
        DistributeAnimation = 5,
        DistributeNOInfo = 6,
        DistributePickUpItem = 7,
        DistributeInitialPos = 9
    };
    public enum ClientPackets
    {
        Test_Packet = 0,
        SendPosition = 1,
        Ready = 2,
        SendAnimationState = 3,
        SendNOInfo = 4,
        PickUpItem = 5
    };
}

