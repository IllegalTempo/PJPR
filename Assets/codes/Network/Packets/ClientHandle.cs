using Assets.codes;
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
    public static void UpdateReadyState(ReadyState state)
    {
        int readyState = (int)state;
        NetworkSystem.Instance.initState = readyState;
        ClientSend.SendReadyState(readyState);
    }
    public static async void test(Connection c, packet packet)
    {
        ulong NetworkID = packet.Readulong();
        if(NetworkID != SteamClient.SteamId)
        {
            Debug.Log("SteamID mismatched, handshake failed");
            NetworkSystem.Instance.Client.Close();
            
        }
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
            NetworkSystem.Instance.Client.Close();

        }
        await Task.Delay(5);
        ClientSend.test();
    }
    public static void DistributePickUpItem(Connection c, packet packet)
    {


        string itemid = packet.ReadstringUNICODE();
        ulong whopicked = packet.Readulong();

        NetworkSystem.Instance.FindNetworkObject[itemid].gameObject.GetComponent<Item>().Network_onChangeOwnership(whopicked);
        Debug.Log($"Received PickUp Item Info: {itemid} picked up by {whopicked}");


    }
    public static async void SyncPlayer(Connection c, packet packet)
    {
        int numplayer = packet.Readint();
        GameClient client = NetworkSystem.Instance.Client;
        for (int i = 0; i < numplayer; i++)
        {
            ulong steamid = packet.Readulong();
            NetworkSystem.Instance.Client.NewPlayer(steamid).Forget();
        }
        await Task.Delay(1000);
        UpdateReadyState(ReadyState.SyncPlayer);


    }
    public static void NewPlayerJoin(Connection c, packet packet)
    {
        ulong playerid = packet.Readulong();
        //int supposeNetworkID = packet.Readint();
        NetworkSystem.Instance.Client.NewPlayer(playerid).Forget();

    }
    public static void PlayerQuit(Connection c, packet packet)
    {
        GameClient cl = NetworkSystem.Instance.Client;
        ulong steamID = packet.Readulong();
        cl.PlayerQuit(steamID);
    }

    public static void ReceivedPlayerMovement(Connection c, packet packet)
    {
        ulong steamID = packet.Readulong();

        Vector3 pos = packet.Readvector3();
        Quaternion headrot = packet.Readquaternion();
        Quaternion bodyrot = packet.Readquaternion();
        NetworkSystem.Instance.PlayerList[steamID].SetMovement(pos, headrot, bodyrot);
    }


    public static void ReceivedPlayerAnimation(Connection c, packet packet)
    {
        ulong NetworkID = packet.Readulong();
        float x = packet.Readfloat();
        float y = packet.Readfloat();
        NetworkSystem.Instance.PlayerList[NetworkID].SetAnimation(x, y);
    }

    public static void DistributeNOInfo(Connection c, packet packet)
    {


        string uid = packet.ReadstringUNICODE();
        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();
        if (NetworkSystem.Instance.FindNetworkObject.ContainsKey(uid))
        {
            NetworkSystem.Instance.FindNetworkObject[uid].SetMovement(pos, rot);
        }
        else
        {
            throw new NO_Not_Found(uid);
        }
        Debug.Log($"[Client] NOINFO Received: [{uid}] [{pos}] [{rot}]");


    }

    public static void NewObject(Connection c, packet packet)
    {
        string prefabID = packet.ReadstringUNICODE();
        string uid = packet.ReadstringUNICODE();
        Vector3 spawnLocation = packet.Readvector3();
        Quaternion spawnRot = packet.Readquaternion();
        ulong owner = packet.Readulong();
        GameCore.Instance.spawnNetworkPrefab(prefabID, owner, uid, spawnLocation, spawnRot).Forget();
    }

    public static void DistributeNOactive(Connection c, packet packet)
    {
        string uid = packet.ReadstringUNICODE();    
        bool active = packet.Readbool();
        if (NetworkSystem.Instance.FindNetworkObject.ContainsKey(uid))
        {
            NetworkSystem.Instance.FindNetworkObject[uid].gameObject.SetActive(active);
        }
        else
        {
            throw new NO_Not_Found(uid);
        }
    }

    public static async void SyncNetworkObjects(Connection c, packet packet)
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
            GameCore.Instance.spawnNetworkPrefab(prefabID, owner, uid, spawnLocation, spawnRot).Forget();
        }
        UpdateReadyState(ReadyState.SyncNetworkObjects);

    }

    public static void DistributeInteract(Connection c, packet packet)
    {
        ulong whoInteracted = packet.Readulong();
        string decorationUID = packet.ReadstringUNICODE();

        IUsable decoration = NetworkSystem.Instance.FindNetworkObject[decorationUID].GetComponent<IUsable>();
        PlayerMain who = NetworkSystem.Instance.PlayerList[whoInteracted].playerControl;
        decoration.OnInteract(who);
    }

    public static void DistributeVoicePacket(Connection c, packet packet)
    {
        ulong whoInteracted = packet.Readulong();
        byte[] bytearray = packet.ReadBytesArray();
        PlayerMain who = NetworkSystem.Instance.PlayerList[whoInteracted].playerControl;
        who.ReceiveVoice(bytearray);
    }

    public static void SendMissionInfo(Connection c, packet packet)
    {
        // TODO: Read packet data here
        // var data = packet.Read...();
        
        // TODO: Handle the packet
    }

    public static void StartGameLoop(Connection c, packet packet)
    {
        // TODO: Read packet data here
        // var data = packet.Read...();
        
        // TODO: Handle the packet
    }
}
