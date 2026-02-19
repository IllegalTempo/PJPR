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

            };



    public GameServer()
    {
        this.maxplayer = NetworkSystem.instance.MaxPlayer;
        //GetSteamID.Add(0, SteamClient.SteamId);
        onOnline();
        Debug.Log("Created GameServer Object");
    }
    public void onOnline()
    {
        NetworkSystem.instance.IsOnline = true;
        NetworkSystem.instance.IsServer = true;
        ulong steamid = SteamClient.SteamId;
        NetworkSystem.instance.SpawnPlayer(steamid); //Add the server player to the player list
        SpawnSpaceShip(SaveObject.instance.saved_decorations, steamid);


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
    private void ClientConnectionEstablished(ConnectionInfo info)
    {
        Debug.Log("Client Connection Established.");
        NetworkListener.Server_OnPlayerJoining?.Invoke(info);
        NetworkPlayer connectedPlayer = GetPlayer(info);
        players[connectedPlayer.steamId].player = NetworkSystem.instance.SpawnPlayer(connectedPlayer.steamId);
        SpawnSpaceShip(connectedPlayer.steamId);
        ServerSend.test(connectedPlayer); // Send a test to the player along with his networkid
        //When a player enter the server, send them the room info including all current players including himself;
        ServerSend.InitRoomInfo(connectedPlayer, GetPlayerCount()); //Send packet to the one who connects to the server, with room info
        ServerSend.NewPlayerJoined(info); // Broadcast a message to inform all players that a new player has joined
    }
    public override async void OnConnected(Connection connection, ConnectionInfo info)
    {
        base.OnConnected(connection, info);
        Debug.Log(new Friend(info.Identity.SteamId).Name + " is Connected!");
        await Task.Delay(1000);
        ClientConnectionEstablished(info);
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
    public NetworkObject CreateNetworkObject(string prefabID, Vector3 pos, Quaternion rot, ulong owner, Transform parent = null) //Server Only
    { //more check added
        NetworkSystem networkSystem = NetworkSystem.instance;
        if (networkSystem != null && !networkSystem.IsServer) return null;
        string uid = Guid.NewGuid().ToString();

        NetworkObject nobj = GameCore.instance.spawnNetworkPrefab(prefabID, owner, uid, pos, rot, parent);
        if (networkSystem != null && networkSystem.server != null)
        {
            ServerSend.NewObject(prefabID, uid, pos, rot, owner);
        }

        return nobj;

    }
    public Connector SpawnConnector()
    {
        Connector connector = CreateNetworkObject("Spaceship_connector", new Vector3(10, 0, 10), Quaternion.identity, 0).GetComponent<Connector>();
        return connector;

    }
    public Spaceship SpawnSpaceShip(DecorationSaveData[] decs, ulong owner) //run by server
    {
        Spaceship ss = CreateNetworkObject("Spaceship", Vector3.zero, Quaternion.identity, owner).GetComponent<Spaceship>();
        if (decs != null)
        {
            foreach (DecorationSaveData dsd in decs)
            {
                Decoration obj = GameObject.Instantiate(GameCore.instance.GetDecoration(dsd.DecorationID), ss.transform).GetComponent<Decoration>();
                obj.OnCreate(ss, dsd.DecorationPosition, dsd.DecorationRotation);

            }
        }
        else
        {
            Debug.Log("Cannot load decorations");
        }
        return ss;

    }
    public Spaceship SpawnSpaceShip(ulong owner) //run by server itself
    {
        return SpawnSpaceShip(null, owner);
    }
    public override void OnConnecting(Connection connection, ConnectionInfo info)
    {
        base.OnConnecting(connection, info);

        if (NetworkSystem.instance.server.GetPlayerCount() < maxplayer)
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
