using Steamworks;
using Steamworks.Data;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class PacketSend
{
    public static string TestRandomUnicode = "幻想鄉是一個與外界隔絕的神秘之地，其存在自古以來便被視為傳說而流傳。";
    
    public static Result BroadcastPacket(packet p)
    {
        return BroadcastPacket(9999, p); 
    }
    public static Result BroadcastPacket(ConnectionInfo i, packet p)
    {
        return BroadcastPacket(NetworkSystem.instance.server.GetPlayer(i).NetworkID, p);
    }
    public static Result BroadcastPacket(ulong ExcludeID, packet p)
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
    public static Result BroadcastPacketToReady(int excludeid, packet p)
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
