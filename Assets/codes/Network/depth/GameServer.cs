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
    public Dictionary<ulong, NetworkPlayer> players = new Dictionary<ulong, NetworkPlayer>(); //This does not include the server player
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
        this.maxplayer = NetworkSystem.INSTANCE.MaxPlayer;
        Debug.Log("Created GameServer Object");

    }
    public async UniTask<bool> onOnline()
    {
        NetworkSystem.INSTANCE.IsOnline = true;
        NetworkSystem.INSTANCE.IsServer = true;
        ulong steamid = SteamClient.SteamId;
        await NetworkSystem.INSTANCE.SpawnPlayer(steamid); //Add the server player to the player list
        await SpawnSpaceShip(SaveObject.instance.saved_decorations, steamid);
        return true;
        //SpawnConnector();



    }
    public int GetPlayerCount()
    {
        return players.Count + 1;
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
        players[steamid].player = await NetworkSystem.INSTANCE.SpawnPlayer(steamid);
        await SpawnSpaceShip(steamid);
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

        ServerSend.SyncPlayer(connectedPlayer, GetPlayerCount()); //Send packet to the one who connects to the server, with room info
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
        ServerSend.SyncNetworkObjects(connectedPlayer, NetworkSystem.INSTANCE.FindNetworkObject.Values.ToArray()); //Send all network objects to the one who connects to the server, with room info
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
            await network_test_pass.Task.Timeout(TimeSpan.FromSeconds(NetworkSystem.TimeoutSeconds));
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
            await network_test_pass.Task.Timeout(TimeSpan.FromSeconds(NetworkSystem.TimeoutSeconds));
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
        return players[info.Identity.SteamId.Value];
    }
    public NetworkPlayer GetPlayer(ulong steamid)
    {
        return players[steamid];
    }
    //public NetworkPlayer GetPlayer(int NetworkID)
    //{
    //    return players[GetSteamID[NetworkID]];
    //}
    public async UniTask<NetworkObject> CreateNetworkObject(string prefabID, Vector3 pos, Quaternion rot, ulong owner, Transform parent = null, bool dontcreateinInit = false) //Server Only
    { //more check added
        NetworkSystem networkSystem = NetworkSystem.INSTANCE;
        if (networkSystem != null && !networkSystem.IsServer) return null;
        string uid = Guid.NewGuid().ToString();

        NetworkObject nobj = await GameCore.INSTANCE.spawnNetworkPrefab(prefabID, owner, uid, pos, rot, parent);
        ServerSend.NewObject(prefabID, uid, pos, rot, owner);

        return nobj;

    }
    //public Connector SpawnConnector()
    //{
    //    Connector connector = CreateNetworkObject("Spaceship_connector", new Vector3(10, 0, 10), Quaternion.identity, 0).GetComponent<Connector>();
    //    return connector;

    //}
    public async UniTask<Spaceship> SpawnSpaceShip(DecorationSaveData[] decs, ulong owner) //run by server
    {
        Spaceship ss = (await CreateNetworkObject("Spaceship", new Vector3(0, 5, 0), Quaternion.identity, owner)).GetComponent<Spaceship>(); ;
        ss.OwnerPlayer = NetworkSystem.INSTANCE.PlayerList[owner];
        ss.OwnerPlayer.spaceship = ss;
        if (decs != null)
        {
            foreach (DecorationSaveData dsd in decs)
            {
                GameObject prefab = await GameCore.INSTANCE.GetDecoration(dsd.DecorationID);
                Decoration obj = GameObject.Instantiate(prefab, ss.transform).GetComponent<Decoration>();
                obj.OnCreate(ss, dsd.DecorationPosition, dsd.DecorationRotation);

            }
        }
        else
        {
            Debug.Log("Cannot load decorations");
        }
        return ss;

    }
    public async UniTask<Spaceship> SpawnSpaceShip(ulong owner)
    {
        return await SpawnSpaceShip(null, owner);
    }
    public override void OnConnecting(Connection connection, ConnectionInfo info)
    {
        base.OnConnecting(connection, info);

        if (GetPlayerCount() < maxplayer)
        {

            Debug.Log(new Friend(info.Identity.SteamId).Name + " is connecting");
            //int networkid = GetSteamID.Count;
            players.Add(info.Identity.SteamId.Value, new NetworkPlayer(info.Identity.SteamId, connection));

            //GetSteamID.Add(networkid, info.Identity.SteamId.Value);
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
        ulong networkid = whodis.steamId;
        whodis.player.Disconnect();

        players.Remove(info.Identity.SteamId.Value);
        //GetSteamID.Remove(networkid);

        ServerSend.PlayerQuit(networkid);

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
