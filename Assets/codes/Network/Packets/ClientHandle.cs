using Steamworks.Data;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class ClientHandle
{
    public static async void test(Connection c, packet packet)
    {
        ulong NetworkID = packet.Readulong();
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

        NetworkSystem.instance.FindNetworkObject[itemid].gameObject.GetComponent<Item>().Network_onChangeOwnership(whopicked);
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
            NetworkSystem.instance.client.NewPlayer(steamid);



        }
        await Task.Delay(1000);
        ClientSend.ReadyUpdate();
    }
    public static void NewPlayerJoin(Connection c, packet packet)
    {
        ulong playerid = packet.Readulong();
        //int supposeNetworkID = packet.Readint();
        NetworkSystem.instance.client.NewPlayer(playerid);

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


        string uid = packet.ReadstringUNICODE();
        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();
        if (NetworkSystem.instance.FindNetworkObject.ContainsKey(uid))
        {
            NetworkSystem.instance.FindNetworkObject[uid].SetMovement(pos, rot);
        }
        else
        {
            throw new NO_Not_Found(uid);
        }


    }

    public static void NewObject(Connection c, packet packet)
    {
        string prefabID = packet.ReadstringUNICODE();
        string uid = packet.ReadstringUNICODE();
        Vector3 spawnLocation = packet.Readvector3();
        Quaternion spawnRot = packet.Readquaternion();
        ulong owner = packet.Readulong();
        GameCore.instance.spawnNetworkPrefab(prefabID, owner, uid, spawnLocation, spawnRot);
    }

    public static void DistributeNOactive(Connection c, packet packet)
    {
        string uid = packet.ReadstringUNICODE();    
        bool active = packet.Readbool();
        if (NetworkSystem.instance.FindNetworkObject.ContainsKey(uid))
        {
            NetworkSystem.instance.FindNetworkObject[uid].gameObject.SetActive(active);
        }
        else
        {
            throw new NO_Not_Found(uid);
        }
    }

    public static void SyncNetworkObjects(Connection c, packet packet)
    {
        int length = packet.Readint();
        for (int i = 0; i < length; i++)
        {
            string uid = packet.ReadstringUNICODE();
            ulong owner = packet.Readulong();
            string prefabID = packet.ReadstringUNICODE();
            GameCore.instance.spawnNetworkPrefab(prefabID,owner, uid,Vector3.zero,Quaternion.identity);
        }
    }
}
