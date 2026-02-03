using Steamworks;
using Steamworks.Data;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class PacketSend
{
    public enum ServerPackets
    {
        Test_Packet = 0,
        RoomInfoOnPlayerEnterRoom = 1,
        UpdatePlayerEnterRoomForExistingPlayer = 2,
        PlayerQuit = 3,

        DistributeMovement = 4,
        DistributeAnimation = 5,};
    public static string TestRandomUnicode = "幻想鄉是一個與外界隔絕的神秘之地，其存在自古以來便被視為傳說而流傳。";
    public static Result Server_Send_test(NetworkPlayer pl)
    {
        using (packet p = new packet((int)ServerPackets.Test_Packet))
        {
            p.Write(pl.NetworkID);
            p.WriteUNICODE(TestRandomUnicode);
            Debug.Log("sending: " + DateTime.Now.Ticks);
            p.Write(DateTime.Now.Ticks);
            return pl.SendPacket(p);

        };
    }
    

    
    public static Result Server_DistributeMovement(int SourceNetworkID, Vector3 pos, Quaternion headrot, Quaternion bodyrot)
    {
        using (packet p = new packet((int)ServerPackets.DistributeMovement))
        {
            p.Write(SourceNetworkID);
            p.Write(pos);
            p.Write(headrot);
            p.Write(bodyrot);
            return BroadcastPacketToReady(SourceNetworkID,p);

        };
    }
    public static Result Server_DistributePlayerAnimationState(int SourceNetworkID,float movementx,float movementy)
    {
        using (packet p = new packet((int)ServerPackets.DistributeAnimation))
        {
            p.Write(SourceNetworkID);
            p.Write(movementx);
            p.Write(movementy);

            return BroadcastPacketToReady(SourceNetworkID, p);
        };
    }
    
    public static Result Server_Send_NewPlayerJoined(ConnectionInfo newplayer)
    {
        using (packet p = new packet((int)ServerPackets.UpdatePlayerEnterRoomForExistingPlayer))
        {
            p.Write(newplayer.Identity.SteamId);
            p.Write(NetworkSystem.instance.server.GetPlayer(newplayer).NetworkID);
            return BroadcastPacket(newplayer, p);

        };
    }
    public static Result Server_Send_PlayerQuit(int NetworkID) //who quitted
    {
        using (packet p = new packet((int)ServerPackets.PlayerQuit))
        {
            p.Write(NetworkID);

            return BroadcastPacket(p);

        };
    }
    private static Result BroadcastPacket(packet p)
    {
        return BroadcastPacket(9999, p);
    }
    private static Result BroadcastPacket(ConnectionInfo i, packet p)               
    {
        return BroadcastPacket( NetworkSystem.instance.server.GetPlayer(i).NetworkID, p);
    }
    private static Result BroadcastPacket(ulong ExcludeID, packet p)
    {
        return BroadcastPacket(NetworkSystem.instance.server.players[ExcludeID].NetworkID, p);
    }
    private static Result BroadcastPacket(int excludeid, packet p)
    {
        for (int i = 1; i < NetworkSystem.instance.server.GetPlayerCount(); i++)
        {
            NetworkPlayer sendtarget = NetworkSystem.instance.server.GetPlayerByIndex(i);
            if (sendtarget.NetworkID != excludeid)
            {
                if (sendtarget.SendPacket(p) != Result.OK)
                {
                    Debug.Log("Result Error when broadcasting packet");
                    return Result.Cancelled;
                }
            }

        }
        return Result.OK;
    }
    private static Result BroadcastPacketToReady(int excludeid, packet p)
    {
        if (NetworkSystem.instance.server == null) return Result.Disabled;
        int playercount = NetworkSystem.instance.server.GetPlayerCount();
        for (int i = 1; i < playercount; i++)
        {
            NetworkPlayer sendtarget = NetworkSystem.instance.server.GetPlayerByIndex(i);
            if (sendtarget.NetworkID != excludeid && sendtarget.MovementUpdateReady)
            {
                if (sendtarget.SendPacket(p) != Result.OK)
                {
                    Debug.Log("Result Error when broadcasting packet");
                    return Result.Cancelled;
                }
            }

        }
        return Result.OK;
    }
    public static Result Server_Send_InitRoomInfo(NetworkPlayer target, int NumPlayer)
    {
        using (packet p = new packet((int)ServerPackets.RoomInfoOnPlayerEnterRoom))
        {
            p.Write(NumPlayer);
            for (int i = 0; i < NumPlayer; i++)
            {
                p.Write(NetworkSystem.instance.server.GetSteamID.ElementAt(i).Key);
                p.Write(NetworkSystem.instance.server.GetSteamID[i]); //given information (SteamID)
            }
            return target.SendPacket(p);
        }
    }



    //Area for client Packets!
    public enum ClientPackets
    {
        Test_Packet = 0,
        SendPosition = 1,
        Ready = 2,
        SendAnimationState = 3,};
    public static Result Client_Send_AnimationState(float movementx,float movementy)
    {
        using (packet p = new packet((int)ClientPackets.SendAnimationState))
        {
            p.Write(movementx);
            p.Write(movementy);
            return SendToServer(p);
        }
    }
    
    public static Result Client_Send_test()
    {
        using (packet p = new packet((int)ClientPackets.Test_Packet))
        {
            p.WriteUNICODE(TestRandomUnicode);
            p.Write(DateTime.Now.Ticks);
            Debug.Log("sending: " + DateTime.Now.Ticks);

            return SendToServer(p);


        };
    }
    
    public static Result Client_Send_ReadyUpdate()
    {
        Debug.Log("Send Ready");
        using (packet p = new packet((int)ClientPackets.Ready))
        {
            p.Write(true);

            return SendToServer(p);


        };
    }
    public static Result Client_Send_Position(Vector3 pos,Quaternion cameraRotation, Quaternion BodyRotation)
    {
        using (packet p = new packet((int)ClientPackets.SendPosition))
        {
            p.Write(pos);
            p.Write(cameraRotation);
            p.Write(BodyRotation);
            return SendToServer(p);
        }
    }
    private static Result SendToServer(packet p)
    {
        Connection server = NetworkSystem.instance.client.GetServer();

        // Fix: Check for default value instead of null for structs
        if (server.Equals(default(Connection)))
        {
            return Result.ConnectFailed;
        }
        else
        {
            return PacketSendingUtils.SendPacketToConnection(NetworkSystem.instance.client.GetServer(), p);
        }
    }












}

public class PacketSendingUtils
{
    public static Result SendPacketToConnection(Connection c, packet p)
    {
        byte[] data = p.GetPacketData();
        IntPtr datapointer = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, datapointer, data.Length);
        Result r = c.SendMessage(datapointer, data.Length, SendType.Reliable);
        Marshal.FreeHGlobal(datapointer); //Free memory allocated
        return r;
    }
    

}
