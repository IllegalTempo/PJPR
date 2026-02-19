using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;

public class NetworkPlayer
{
    public Connection connection;
    public string SteamName;
    public SteamId steamId;
    //public int NetworkID;
    public bool IsLocal;
    public NetworkPlayerObject player;
    public bool init = false;
    public NetworkPlayer(SteamId steamid
        //,int NetworkID
        ,Connection connection)
    {
        //this.NetworkID = NetworkID;
        SteamName = (new Friend(steamid)).Name;
        this.connection = connection;
        this.steamId = steamid;
    }
    private Result SendData(byte[] data)
    {
        IntPtr datapointer = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, datapointer, data.Length);
        Result r = connection.SendMessage(datapointer, data.Length,SendType.Reliable);
        Marshal.FreeHGlobal(datapointer);
        return r;
        
    }
    public Result SendPacket(packet p)
    {
        return SendData(p.GetPacketData());
    }
    public void onReady(bool ready) //when Init Room is done, (Player is spawned)
    {
        if (!ready) return;
        init = ready;
        ServerSend.SyncNetworkObjects(this, NetworkSystem.instance.FindNetworkObject.Values.Where(x=>!x.InScene).ToArray()); //TODO: If array too long, split into multiple packets

    }
}
