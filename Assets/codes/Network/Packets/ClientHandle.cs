using Assets.codes.items;
using Cysharp.Threading.Tasks;
using Steamworks;
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

        NetworkSystem.INSTANCE.FindNetworkObject[itemid].gameObject.GetComponent<Item>().Network_onChangeOwnership(whopicked);
        Debug.Log($"Received PickUp Item Info: {itemid} picked up by {whopicked}");


    }



    public static void DistributeInitialPos(Connection c, packet packet)
    {


        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();

        GameCore.INSTANCE.Local_Player.transform.position = pos;
        GameCore.INSTANCE.Local_Player.transform.rotation = rot;
        Debug.Log("Received Initial Pos and Rot");

    }
    public static async void InitRoomInfo(Connection c, packet packet)
    {
        int numplayer = packet.Readint();
        GameClient client = NetworkSystem.INSTANCE.Client;
        for (int i = 0; i < numplayer; i++)
        {
            ulong steamid = packet.Readulong();
            NetworkSystem.INSTANCE.Client.NewPlayer(steamid).Forget();
            


        }
        await Task.Delay(1000);
        NetworkSystem.INSTANCE.initRoom = true;

        ClientSend.ReadyUpdate();
    }
    public static void NewPlayerJoin(Connection c, packet packet)
    {
        ulong playerid = packet.Readulong();
        //int supposeNetworkID = packet.Readint();
        NetworkSystem.INSTANCE.Client.NewPlayer(playerid).Forget();

    }
    public static void PlayerQuit(Connection c, packet packet)
    {
        GameClient cl = NetworkSystem.INSTANCE.Client;
        ulong steamID = packet.Readulong();
        cl.PlayerQuit(steamID);
    }

    public static void ReceivedPlayerMovement(Connection c, packet packet)
    {
        ulong steamID = packet.Readulong();

        Vector3 pos = packet.Readvector3();
        Quaternion headrot = packet.Readquaternion();
        Quaternion bodyrot = packet.Readquaternion();
        NetworkSystem.INSTANCE.PlayerList[steamID].SetMovement(pos, headrot, bodyrot);
    }


    public static void ReceivedPlayerAnimation(Connection c, packet packet)
    {
        ulong NetworkID = packet.Readulong();
        float x = packet.Readfloat();
        float y = packet.Readfloat();
        NetworkSystem.INSTANCE.PlayerList[NetworkID].SetAnimation(x, y);
    }

    public static void DistributeNOInfo(Connection c, packet packet)
    {


        string uid = packet.ReadstringUNICODE();
        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();
        if (NetworkSystem.INSTANCE.FindNetworkObject.ContainsKey(uid))
        {
            NetworkSystem.INSTANCE.FindNetworkObject[uid].SetMovement(pos, rot);
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
        GameCore.INSTANCE.spawnNetworkPrefab(prefabID, owner, uid, spawnLocation, spawnRot).Forget();
    }

    public static void DistributeNOactive(Connection c, packet packet)
    {
        string uid = packet.ReadstringUNICODE();    
        bool active = packet.Readbool();
        if (NetworkSystem.INSTANCE.FindNetworkObject.ContainsKey(uid))
        {
            NetworkSystem.INSTANCE.FindNetworkObject[uid].gameObject.SetActive(active);
        }
        else
        {
            throw new NO_Not_Found(uid);
        }
    }

    public static void SyncNetworkObjects(Connection c, packet packet)
    {
        int length = packet.Readint();
        Debug.Log($"Syncing {length} Network Objects from Server");
        for (int i = 0; i < length; i++)
        {
            string uid = packet.ReadstringUNICODE();
            ulong owner = packet.Readulong();
            string prefabID = packet.ReadstringUNICODE();
            Vector3 spawnLocation = packet.Readvector3();
            Quaternion spawnRot = packet.Readquaternion();
            GameCore.INSTANCE.spawnNetworkPrefab(prefabID,owner, uid,spawnLocation,Quaternion.identity).Forget();
        }
    }

    public static void DistributeDecorationInteract(Connection c, packet packet)
    {
        ulong whoInteracted = packet.Readulong();
        string decorationUID = packet.ReadstringUNICODE();

        IUsable decoration = NetworkSystem.INSTANCE.FindNetworkObject[decorationUID].GetComponent<IUsable>();
        PlayerMain who = NetworkSystem.INSTANCE.PlayerList[whoInteracted].playerControl;
        decoration.OnInteract(who);
    }
}
