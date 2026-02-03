using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.VisualScripting;

public class GameServer : SocketManager
{
    public int maxplayer;
    public Dictionary<ulong, NetworkPlayer> players = new Dictionary<ulong, NetworkPlayer>(); //This does not include the server player
    public Dictionary<int, ulong> GetSteamID = new Dictionary<int, ulong>();
    private delegate void PacketHandle(NetworkPlayer n, packet p);




    private Dictionary<int, PacketHandle> ServerPacketHandles = new Dictionary<int, PacketHandle>()
        {
            { (int)PacketSend.ClientPackets.Test_Packet,PacketHandles_Method.Server_Handle_test },
            { (int)PacketSend.ClientPackets.SendPosition,PacketHandles_Method.Server_Handle_PosUpdate},
            { (int)PacketSend.ClientPackets.SendAnimationState,PacketHandles_Method.Server_Handle_AnimationState},
            { (int)PacketSend.ClientPackets.Ready,PacketHandles_Method.Server_Handle_ReadyUpdate}
            
            
            ,
            { (int)PacketSend.ClientPackets.SendNOInfo, PacketHandles_Method.Server_Handle_SendNOInfo }
        ,
            { (int)PacketSend.ClientPackets.PickUpItem, PacketHandles_Method.Server_Handle_PickUpItem }
        };



    public GameServer()
    {
        this.maxplayer = NetworkSystem.instance.MaxPlayer;
        GetSteamID.Add(0, SteamClient.SteamId);
        GameObject g = NetworkSystem.instance.SpawnPlayer(true, 0, SteamClient.SteamId).gameObject;

        Debug.Log("Created GameServer Object");
    }
    public int GetPlayerCount()
    {
        return GetSteamID.Count;
    }
    public void DisconnectAll()
    {
        players.Clear();
        foreach (Connection item in Connected)
        {
            item.Close();
        }
    }
    public Result SendPacket(ulong steamid, packet p)
    {
        return players[steamid].SendPacket(p);
    }

    public override async void OnConnected(Connection connection, ConnectionInfo info)
    {
        base.OnConnected(connection, info);
        Debug.Log(new Friend(info.Identity.SteamId).Name + " is Connected!");
        await Task.Delay(1000);
        Debug.Log("Sending Test Packet");
        NetworkListener.Server_OnPlayerJoining?.Invoke(info);
        NetworkPlayer connectedPlayer = GetPlayer(info);
        players[connectedPlayer.steamId].player = NetworkSystem.instance.SpawnPlayer(false, connectedPlayer.NetworkID, connectedPlayer.steamId);

        PacketSend.Server_Send_test(connectedPlayer); // Send a test to the player along with his networkid
        //When a player enter the server, send them the room info including all current players including himself;
        PacketSend.Server_Send_InitRoomInfo(connectedPlayer, GetPlayerCount()); //Send packet to the one who connects to the server, with room info

        PacketSend.Server_Send_NewPlayerJoined(info); // Broadcast a message to inform all players that a new player has joined
    }
    public NetworkPlayer GetPlayer(ConnectionInfo info)
    {
        return players[info.Identity.SteamId.Value];
    }
    public NetworkPlayer GetPlayerByIndex(int index)
    {
        return players.ElementAt(index-1).Value;
    }
    public NetworkPlayer GetPlayer(int NetworkID)
    {
        return players[GetSteamID[NetworkID]];
    }
    public override void OnConnecting(Connection connection, ConnectionInfo info)
    {
        base.OnConnecting(connection, info);

        if (NetworkSystem.instance.server.GetPlayerCount() < maxplayer)
        {

            Debug.Log(new Friend(info.Identity.SteamId).Name + " is connecting");
            int networkid = GetSteamID.Count;
            players.Add(info.Identity.SteamId.Value, new NetworkPlayer(info.Identity.SteamId, networkid, connection));

            GetSteamID.Add(networkid, info.Identity.SteamId.Value);
            Debug.Log(players.Count);
            connection.Accept();
        }
        else
        {
            Debug.Log(new Friend(info.Identity.SteamId).Name + " cannot connected as the server is full");
            connection.Close();
        }


    }
    public override void OnDisconnected(Connection connection, ConnectionInfo info)
    {
        base.OnDisconnected(connection, info);
        Debug.Log(new Friend(info.Identity.SteamId).Name + " is Disconnected.");
        NetworkPlayer whodis = players[info.Identity.SteamId];
        int networkid = whodis.NetworkID;
        whodis.player.Disconnect();
        
        players.Remove(info.Identity.SteamId.Value);
        GetSteamID.Remove(networkid);

        PacketSend.Server_Send_PlayerQuit(networkid);

    }
    public override unsafe void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);
        byte[] bytedata = new byte[size];
        Marshal.Copy(data, bytedata, 0, size);
        using (packet packet = new packet(bytedata))
        {

            ServerPacketHandles[packet.Readint()](players[identity.SteamId], packet);

        }
    }

}
