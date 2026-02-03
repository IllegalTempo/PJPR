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
    public int NetworkID;
    private delegate void PacketHandle(Connection c, packet p);
    public Dictionary<int, NetworkPlayerObject> GetPlayerByNetworkID = new Dictionary<int, NetworkPlayerObject>();



    private Dictionary<int, PacketHandle> ClientPacketHandles = new Dictionary<int, PacketHandle>()
        {
            { (int)PacketSend.ServerPackets.Test_Packet,PacketHandles_Method.Client_Handle_test },
            { (int)PacketSend.ServerPackets.RoomInfoOnPlayerEnterRoom,PacketHandles_Method.Client_Handle_InitRoomInfo },
            { (int)PacketSend.ServerPackets.UpdatePlayerEnterRoomForExistingPlayer,PacketHandles_Method.Client_Handle_NewPlayerJoin },
            { (int)PacketSend.ServerPackets.PlayerQuit,PacketHandles_Method.Client_Handle_PlayerQuit },
            { (int)PacketSend.ServerPackets.DistributeMovement,PacketHandles_Method.Client_Handle_ReceivedPlayerMovement},
            { (int)PacketSend.ServerPackets.DistributeAnimation,PacketHandles_Method.Client_Handle_ReceivedPlayerAnimation},
            { (int)PacketSend.ServerPackets.DistributeNOInfo, PacketHandles_Method.Client_Handle_DistributeNOInfo },
            { (int)PacketSend.ServerPackets.DistributePickUpItem, PacketHandles_Method.Client_Handle_DistributePickUpItem },
            { (int)PacketSend.ServerPackets.DistributeInitialPos, PacketHandles_Method.Client_Handle_DistributeInitialPos }
        };


    public bool IsLocal(int id)
    {
        return id == NetworkID;
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
        Debug.Log("Disconnected to " + new Friend(info.Identity.SteamId).Name + " " + info.EndReason + " " + info.State + " " + info.Address.Address.ToString());
        NetworkSystem.instance.CreateGameLobby();

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
