using Steamworks.Data;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class ClientHandle
{
    public static async void test(Connection c, packet packet)
    {
        //int NetworkID = packet.Readint();
        string text = packet.ReadstringUNICODE();
        long Servertime = packet.Readlong();
        //NetworkSystem.instance.client.NetworkID = NetworkID;

        if (text == PacketSend.TestRandomUnicode)
        {
            Debug.Log($"{Servertime} Confirmed connected from server. delay:{(DateTime.Now.Ticks - Servertime) / 10000}ms");

        }
        else
        {
            Debug.Log($"Check Code Mismatched Server Message: {text}");

        }
        await Task.Delay(5);
        ClientSend.test();
    }
    public static void DistributePickUpItem(Connection c, packet packet)
    {


        string itemid = packet.ReadstringUNICODE();
        ulong whopicked = packet.Readulong();

        NetworkSystem.instance.FindNetworkObject[itemid].Network_ChangeOwner(whopicked);
        Debug.Log($"Received PickUp Item Info: {itemid} picked up by {whopicked}");


    }



    public static void DistributeInitialPos(Connection c, packet packet)
    {


        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();

        GameCore.instance.localPlayer.transform.position = pos;
        GameCore.instance.localPlayer.transform.rotation = rot;
        Debug.Log("Received Initial Pos and Rot");

    }
    public static async void InitRoomInfo(Connection c, packet packet)
    {
        int numplayer = packet.Readint();
        GameClient client = NetworkSystem.instance.client;
        for (int i = 0; i < numplayer; i++)
        {
            ulong steamid = packet.Readulong();
            Debug.Log($"Spawning Player {steamid}");
            client.GetPlayerBySteamID.Add(steamid, NetworkSystem.instance.SpawnPlayer(client.IsLocal(steamid), steamid));


        }
        await Task.Delay(1000);
        ClientSend.ReadyUpdate();
    }
    public static void NewPlayerJoin(Connection c, packet packet)
    {
        ulong playerid = packet.Readulong();
        //int supposeNetworkID = packet.Readint();




        NetworkSystem.instance.client.GetPlayerBySteamID.Add(playerid, NetworkSystem.instance.SpawnPlayer(false, playerid));
    }
    public static void PlayerQuit(Connection c, packet packet)
    {
        GameClient cl = NetworkSystem.instance.client;
        ulong steamID = packet.Readulong();
        cl.GetPlayerBySteamID[steamID].Disconnect();
        cl.GetPlayerBySteamID.Remove(steamID);
    }

    public static void ReceivedPlayerMovement(Connection c, packet packet)
    {
        ulong steamID = packet.Readulong();

        Vector3 pos = packet.Readvector3();
        Quaternion headrot = packet.Readquaternion();
        Quaternion bodyrot = packet.Readquaternion();
        NetworkSystem.instance.client.GetPlayerBySteamID[steamID].SetMovement(pos, headrot, bodyrot);
    }


    public static void ReceivedPlayerAnimation(Connection c, packet packet)
    {
        ulong NetworkID = packet.Readulong();
        float x = packet.Readfloat();
        float y = packet.Readfloat();
        NetworkSystem.instance.client.GetPlayerBySteamID[NetworkID].SetAnimation(x, y);
    }

    public static void DistributeNOInfo(Connection c, packet packet)
    {


        string uuid = packet.ReadstringUNICODE();
        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();
        NetworkSystem.instance.FindNetworkObject[uuid].SetMovement(pos, rot);


    }

    public static void NewObject(Connection c, packet packet)
    {
        string prefabID = packet.ReadstringUNICODE();
        string uid = packet.ReadstringUNICODE();
        Vector3 spawnLocation = packet.Readvector3();
        Quaternion spawnRot = packet.Readquaternion();
        NetworkSystem.instance.Client_ReceivedNewNetworkObject(prefabID,uid, spawnLocation,spawnRot);
    }
}
