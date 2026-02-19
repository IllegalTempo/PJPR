using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System;
using Connection = Steamworks.Data.Connection;
using System.Runtime.InteropServices;

public class GameClient : ConnectionManager
{
    //public int NetworkID;
    private delegate void PacketHandle(Connection c, packet p);
    public Dictionary<ulong, NetworkPlayerObject> GetPlayerBySteamID = new Dictionary<ulong, NetworkPlayerObject>();
    
    public GameClient()
    {
    }

    private Dictionary<int, PacketHandle> ClientPacketHandles = new Dictionary<int, PacketHandle>()
        {
            { (int)packets.ServerPackets.Test_Packet, ClientHandle.test },
            { (int)packets.ServerPackets.RoomInfoOnPlayerEnterRoom, ClientHandle.InitRoomInfo },
            { (int)packets.ServerPackets.UpdatePlayerEnterRoomForExistingPlayer, ClientHandle.NewPlayerJoin },
            { (int)packets.ServerPackets.PlayerQuit, ClientHandle.PlayerQuit },
            { (int)packets.ServerPackets.DistributeMovement, ClientHandle.ReceivedPlayerMovement },
            { (int)packets.ServerPackets.DistributeAnimation, ClientHandle.ReceivedPlayerAnimation },
            { (int)packets.ServerPackets.DistributeNOInfo, ClientHandle.DistributeNOInfo },
            { (int)packets.ServerPackets.DistributePickUpItem, ClientHandle.DistributePickUpItem },
            { (int)packets.ServerPackets.DistributeInitialPos, ClientHandle.DistributeInitialPos }
            ,
            { (int)packets.ServerPackets.NewObject, ClientHandle.NewObject }
        
            
            ,
            { (int)packets.ServerPackets.DistributeNOactive, ClientHandle.DistributeNOactive }
        ,
            { (int)packets.ServerPackets.SyncNetworkObjects, ClientHandle.SyncNetworkObjects }
        };

    public void NewPlayer(ulong who)
    {
        Debug.Log($"Spawning Player {who} and spaceship");
        GetPlayerBySteamID.Add(who, NetworkSystem.instance.SpawnPlayer(false, who));
    }
    public bool IsLocal(ulong id)
    {
        return id == NetworkSystem.instance.PlayerId;
    }
    public Connection GetServer()
    {
        return Connection;
    }
    public override void OnConnected(ConnectionInfo info)
    {
        base.OnConnected(info);
        Debug.Log("Successfully Connected to " + info.Identity.SteamId);

    }
    public override void OnConnecting(ConnectionInfo info)
    {
        base.OnConnecting(info);
        Debug.Log("Connecting to " + info.Identity.SteamId + "...");


    }
    public override void OnDisconnected(ConnectionInfo info)
    {
        base.OnDisconnected(info);
        Debug.Log("Disconnected from " + new Friend(info.Identity.SteamId).Name + " " + info.EndReason + " " + info.State + " " + info.Address.Address.ToString());
        NetworkSystem.instance.resetGame();
        if (NetworkSystem.instance.CreateLobbyOnStart)
        {
            NetworkSystem.instance.CreateGameLobby();

        }

    }
    public override unsafe void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(data, size, messageNum, recvTime, channel);


        byte* bytepointer = (byte*)data.ToPointer();
        byte[] bytedata = new byte[size];
        Marshal.Copy(data, bytedata, 0, size);
        using (packet packet = new packet(bytedata))
        {
            int packetid = packet.Readint();
            PacketHandle handle;
            if (ClientPacketHandles.TryGetValue(packetid,out handle))
            {
                handle(Connection, packet);
            } else
            {
                Debug.Log($"Packet ID: {packetid} not found");
            }

        }
    }
}
