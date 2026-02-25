using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using static UnityEngine.AdaptivePerformance.Provider.AdaptivePerformanceSubsystemDescriptor;
using static UnityEngine.UI.GridLayoutGroup;

public class GameServer : SocketManager
{
    public int maxplayer;
    public Dictionary<ulong, NetworkPlayer> NetworkUsers = new Dictionary<ulong, NetworkPlayer>(); //This does not include the server player
    //public Dictionary<int, ulong> GetSteamID = new Dictionary<int, ulong>();
    private delegate void PacketHandle(NetworkPlayer n, packet p);


    private Dictionary<int, PacketHandle> ServerPacketHandles = new Dictionary<int, PacketHandle>()
        {
            { (int)packets.ClientPackets.Test_Packet,ServerHandle.test },
            { (int)packets.ClientPackets.SendPosition,ServerHandle.PosUpdate},
            { (int)packets.ClientPackets.SendAnimationState,ServerHandle.AnimationState},
            { (int)packets.ClientPackets.SendNOInfo, ServerHandle.SendNOInfo },
            { (int)packets.ClientPackets.PickUpItem, ServerHandle.PickUpItem }

            ,
            { (int)packets.ClientPackets.SendDecorationInteract, ServerHandle.SendDecorationInteract }
        ,
            { (int)packets.ClientPackets.SendReadyState, ServerHandle.SendReadyState }
        };



    public GameServer()
    {
        this.maxplayer = NetworkSystem.Instance.MaxPlayer;
        Debug.Log("Created GameServer Object");

    }
    public async UniTask<bool> onOnline()
    {
        NetworkSystem system = NetworkSystem.Instance;
        system.IsOnline = true;
        system.IsServer = true;
        await system.StartAsHost();
        return true;
    }
    
    public int GetUserCount()
    {
        return NetworkUsers.Count + 1;
    }
    public void DisconnectAll()
    {
        NetworkUsers.Clear();
        foreach (Connection item in Connected)
        {
            item.Close();
        }
    }
    private async UniTask<bool> ClientConnectionEstablished(ConnectionInfo info)
    {
        Debug.Log("Client Connection Established.");
        NetworkListener.RaisePlayerJoining(info);
        NetworkPlayer connectedPlayer = GetPlayer(info);

        await InstantiatePlayerToServer(info);
        bool success = await SyncPlayer(connectedPlayer);
        ServerSend.NewPlayerJoined(info); // Broadcast a message to inform all players that a new player has joined

        return success;
    }
    private async UniTask InstantiatePlayerToServer(ConnectionInfo info)
    {
        ulong steamid = info.Identity.SteamId;
        NetworkSystem system = NetworkSystem.Instance;
        NetworkUsers[steamid].player = await system.SpawnPlayer(steamid);
        await system.SpawnSpaceShip(steamid,NetworkUsers.Count-1);
        Debug.Log($"Player {steamid} instantiated on server.");
    }
    private async UniTask<bool> SyncPlayer(NetworkPlayer connectedPlayer)
    {

        UniTaskCompletionSource player_spawned = new UniTaskCompletionSource();

        bool testPass = await WaitForTestSuccess(connectedPlayer);
        if (testPass)
        {
            Debug.Log($"Player {connectedPlayer.steamId} passed the network test.");

        }
        else
        {
            Debug.Log($"Player {connectedPlayer.steamId} failed the network test. Disconnecting...");
            connectedPlayer.connection.Close();
            return false;
        }

        ServerSend.SyncPlayer(connectedPlayer, GetUserCount()); //Send packet to the one who connects to the server, with room info
        bool syncplayer = await WaitForReadyState(connectedPlayer, (int)ReadyState.SyncPlayer);
        if (syncplayer)
        {
            Debug.Log($"Player {connectedPlayer.steamId} is ready to receive player data.");
        }
        else
        {
            Debug.Log($"Player {connectedPlayer.steamId} failed to sync player data. Disconnecting...");
            connectedPlayer.connection.Close();
            return false;
        }
        ServerSend.SyncNetworkObjects(connectedPlayer, NetworkSystem.Instance.FindNetworkObject.Values.Where(x => !x.InScene).ToArray());
        Debug.Log($"Sent network objects to player {connectedPlayer.steamId}.");
        return true;
    }
    private async UniTask<bool> WaitForTestSuccess(NetworkPlayer connectedPlayer)
    {
        ServerSend.test(connectedPlayer); // Send a test to the player along with his networkid

        var network_test_pass = new UniTaskCompletionSource();
        NetworkListener.Server_OnPlayerJoinSuccessful += onCallback;


        void onCallback(NetworkPlayer pl)
        {
            if (connectedPlayer != pl) return;
            NetworkListener.Server_OnPlayerJoinSuccessful -= onCallback;
            network_test_pass.TrySetResult();

        }
        try
        {
            await network_test_pass.Task.Timeout(TimeSpan.FromSeconds(NetworkSystem.TIMEOUTSECONDS));
            return true;// Wait for the player to respond to the test packet, with a timeout of 5 seconds
        }
        catch (TimeoutException)
        {
            return false;

        }
        finally
        {
            NetworkListener.Server_OnPlayerJoinSuccessful -= onCallback;

        }

    }
    private async UniTask<bool> WaitForReadyState(NetworkPlayer connectedPlayer,int readystate)
    {
        var network_test_pass = new UniTaskCompletionSource();
        NetworkListener.Server_ReadyStateReceived += onCallback;


        void onCallback(NetworkPlayer pl,int state)
        {
            if (connectedPlayer != pl || readystate != state) return;

            NetworkListener.Server_ReadyStateReceived -= onCallback;
            network_test_pass.TrySetResult();

        }
        try
        {
            await network_test_pass.Task.Timeout(TimeSpan.FromSeconds(NetworkSystem.TIMEOUTSECONDS));
            return true;// Wait for the player to respond to the test packet, with a timeout of 5 seconds
        }
        catch (TimeoutException)
        {
            return false;

        }
        finally
        {
            NetworkListener.Server_ReadyStateReceived -= onCallback;

        }

    }
    public override async void OnConnected(Connection connection, ConnectionInfo info)
    {
        base.OnConnected(connection, info);
        Debug.Log(new Friend(info.Identity.SteamId).Name + " is Connected!");
        await Task.Delay(1000);
        await ClientConnectionEstablished(info);
    }
    public NetworkPlayer GetPlayer(ConnectionInfo info)
    {
        return NetworkUsers[info.Identity.SteamId.Value];
    }
    public NetworkPlayer GetPlayer(ulong steamid)
    {
        return NetworkUsers[steamid];
    }
    
    public override void OnConnecting(Connection connection, ConnectionInfo info)
    {
        base.OnConnecting(connection, info);

        if (GetUserCount() < maxplayer)
        {

            Debug.Log(new Friend(info.Identity.SteamId).Name + " is connecting");
            //int networkid = GetSteamID.Count;
            NetworkUsers.Add(info.Identity.SteamId.Value, new NetworkPlayer(info.Identity.SteamId, connection));

            //GetSteamID.Add(networkid, info.Identity.SteamId.Value);
            Debug.Log(NetworkUsers.Count);
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

        NetworkPlayer whodis = NetworkUsers[info.Identity.SteamId];
        whodis.Disconnect();
        ulong networkid = whodis.steamId;

        NetworkUsers.Remove(networkid);
        //GetSteamID.Remove(networkid);


    }
    public override unsafe void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);
        byte[] bytedata = new byte[size];
        Marshal.Copy(data, bytedata, 0, size);
        using (packet packet = new packet(bytedata))
        {

            ServerPacketHandles[packet.Readint()](NetworkUsers[identity.SteamId], packet);

        }
    }

}
