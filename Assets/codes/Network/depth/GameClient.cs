using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.AdaptivePerformance.Provider.AdaptivePerformanceSubsystemDescriptor;
using Connection = Steamworks.Data.Connection;

public class GameClient : ConnectionManager
{
    //public int NetworkID;
    private delegate void PacketHandle(Connection c, packet p);

    public GameClient()
    {
        NetworkSystem.Instance.IsServer = false;
    }

    private Dictionary<int, PacketHandle> ClientPacketHandles = new Dictionary<int, PacketHandle>()
        {
            { (int)packets.ServerPackets.Test_Packet, ClientHandle.test },
            { (int)packets.ServerPackets.RoomInfoOnPlayerEnterRoom, ClientHandle.SyncPlayer },
            { (int)packets.ServerPackets.UpdatePlayerEnterRoomForExistingPlayer, ClientHandle.NewPlayerJoin },
            { (int)packets.ServerPackets.PlayerQuit, ClientHandle.PlayerQuit },
            { (int)packets.ServerPackets.DistributeMovement, ClientHandle.ReceivedPlayerMovement },
            { (int)packets.ServerPackets.DistributeAnimation, ClientHandle.ReceivedPlayerAnimation },
            { (int)packets.ServerPackets.DistributeNOInfo, ClientHandle.DistributeNOInfo },
            { (int)packets.ServerPackets.DistributePickUpItem, ClientHandle.DistributePickUpItem },
            
            { (int)packets.ServerPackets.NewObject, ClientHandle.NewObject }


            ,
            { (int)packets.ServerPackets.DistributeNOactive, ClientHandle.DistributeNOactive }
        ,
            { (int)packets.ServerPackets.SyncNetworkObjects, ClientHandle.SyncNetworkObjects }
        ,
            { (int)packets.ServerPackets.DistributeInteract, ClientHandle.DistributeInteract }
        
            ,
            { (int)packets.ServerPackets.DistributeVoicePacket, ClientHandle.DistributeVoicePacket }
        
            ,
            { (int)packets.ServerPackets.SendMissionInfo, ClientHandle.SendMissionInfo }
        };

    public async UniTask NewPlayer(ulong who)
    {
        Debug.Log($"Spawning Player {who} and spaceship");
        NetworkPlayerObject player = await NetworkSystem.Instance.SpawnPlayer(who);
    }
    public void PlayerQuit(ulong who)
    {
        Debug.Log($"Player {who} Quit the game");
        if(!NetworkSystem.Instance.PlayerList.ContainsKey(who))
        {
            Debug.Log($"Player {who} not found in GetPlayerBySteamID");
            return;
        }
        NetworkSystem.Instance.PlayerList[who].Disconnect();
        NetworkSystem.Instance.PlayerList.Remove(who);
    }
    public bool IsLocal(ulong id)
    {
        return id == NetworkSystem.Instance.SteamID;
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
        Debug.Log("Connecting to " + info.Identity.SteamId + "..." + "");



    }
    public override void OnDisconnected(ConnectionInfo info)
    {
        base.OnDisconnected(info);
        Disconnect(info).Forget();



    }
    private async UniTask Disconnect(ConnectionInfo info)
    {
        Debug.Log("Disconnected from " + new Friend(info.Identity.SteamId).Name + " " + info.EndReason + " " + info.State + " " + info.Address.Address.ToString());
        if(NetworkSystem.Instance.StartServerOnStart)
        {
            await NetworkSystem.Instance.CreateLobby();
        }

    }
    public override unsafe void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(data, size, messageNum, recvTime, channel);


        byte* bytepointer = (byte*)data.ToPointer();
        byte[] bytedata = new byte[size];
        Marshal.Copy(data, bytedata, 0, size);
        float latency = Time.realtimeSinceStartup * 1000f - recvTime;
        using (packet packet = new packet(bytedata))
        {
            int packetid = packet.Readint();
            PacketHandle handle;
            if (ClientPacketHandles.TryGetValue(packetid, out handle))
            {
                handle(Connection, packet);
            }
            else
            {
                Debug.Log($"Packet ID: {packetid} not found");
            }

        }
    }
}
