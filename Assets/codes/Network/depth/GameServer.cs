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
using Cysharp.Threading.Tasks;

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
            { (int)packets.ClientPackets.Ready,ServerHandle.ReadyUpdate},
            { (int)packets.ClientPackets.SendNOInfo, ServerHandle.SendNOInfo },
            { (int)packets.ClientPackets.PickUpItem, ServerHandle.PickUpItem }

            ,
            { (int)packets.ClientPackets.SendDecorationInteract, ServerHandle.SendDecorationInteract }
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
        await SpawnConnector();
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
        NetworkListener.Server_OnPlayerJoining?.Invoke(info);
        NetworkPlayer connectedPlayer = GetPlayer(info);
        players[connectedPlayer.steamId].player = await NetworkSystem.INSTANCE.SpawnPlayer(connectedPlayer.steamId);
        await SpawnSpaceShip(connectedPlayer.steamId);
        ServerSend.test(connectedPlayer); // Send a test to the player along with his networkid
        //When a player enter the server, send them the room info including all current players including himself;
        ServerSend.InitRoomInfo(connectedPlayer, GetPlayerCount()); //Send packet to the one who connects to the server, with room info
        ServerSend.NewPlayerJoined(info); // Broadcast a message to inform all players that a new player has joined
        return true;
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
    public async UniTask<NetworkObject> CreateNetworkObject(string prefabID, Vector3 pos, Quaternion rot, ulong owner, Transform parent = null) //Server Only
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
        Spaceship ss = (await CreateNetworkObject("Spaceship", new Vector3(0,5,0), Quaternion.identity, owner)).GetComponent<Spaceship>(); ;
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
    public async UniTask<Connector> SpawnConnector()
    {
        Connector connector = (await CreateNetworkObject("Spaceship_connector", new Vector3(0, 0, 0), Quaternion.identity, 0)).GetComponent<Connector>();
        GameCore.INSTANCE.Connector = connector;
        return connector;
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
