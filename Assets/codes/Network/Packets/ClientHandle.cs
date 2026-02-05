using Steamworks.Data;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class ClientHandle
{
    public static async void test(Connection c, packet packet)
    {
        int NetworkID = packet.Readint();
        string text = packet.ReadstringUNICODE();
        long Servertime = packet.Readlong();
        NetworkSystem.instance.client.NetworkID = NetworkID;

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
        // TODO: Read packet data here
        // var data = packet.Read...();
        string itemid = packet.ReadstringUNICODE();
        int whopicked = packet.Readint();

        NetworkSystem.instance.FindNetworkObject[itemid].ReceivedNetwork_PickUp(whopicked);

        // TODO: Handle the packet
    }



    public static void DistributeInitialPos(Connection c, packet packet)
    {
        // TODO: Read packet data here
        // var data = packet.Read...();
        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();

        GameCore.instance.localPlayer.transform.position = pos;
        GameCore.instance.localPlayer.transform.rotation = rot;
        Debug.Log("Received Initial Pos and Rot");
        // TODO: Handle the packet
    }
    public static async void InitRoomInfo(Connection c, packet packet)
    {
        int numplayer = packet.Readint();
        GameClient client = NetworkSystem.instance.client;
        for (int i = 0; i < numplayer; i++)
        {
            int NetworkID = packet.Readint();
            ulong steamid = packet.Readulong();
            Debug.Log($"Spawning Player {NetworkID} {steamid}");
            client.GetPlayerByNetworkID.Add(NetworkID, NetworkSystem.instance.SpawnPlayer(client.IsLocal(NetworkID), NetworkID, steamid));


        }
        await Task.Delay(1000);
        ClientSend.ReadyUpdate();
    }
    public static void NewPlayerJoin(Connection c, packet packet)
    {
        ulong playerid = packet.Readulong();
        int supposeNetworkID = packet.Readint();




        NetworkSystem.instance.client.GetPlayerByNetworkID.Add(supposeNetworkID, NetworkSystem.instance.SpawnPlayer(false, supposeNetworkID, playerid));
    }
    public static void PlayerQuit(Connection c, packet packet)
    {
        GameClient cl = NetworkSystem.instance.client;
        int NetworkID = packet.Readint();
        cl.GetPlayerByNetworkID[NetworkID].Disconnect();
        cl.GetPlayerByNetworkID.Remove(NetworkID);
    }

    public static void ReceivedPlayerMovement(Connection c, packet packet)
    {
        int NetworkID = packet.Readint();

        Vector3 pos = packet.Readvector3();
        Quaternion headrot = packet.Readquaternion();
        Quaternion bodyrot = packet.Readquaternion();
        NetworkSystem.instance.client.GetPlayerByNetworkID[NetworkID].SetMovement(pos, headrot, bodyrot);
    }


    public static void ReceivedPlayerAnimation(Connection c, packet packet)
    {
        int NetworkID = packet.Readint();
        float x = packet.Readfloat();
        float y = packet.Readfloat();
        NetworkSystem.instance.client.GetPlayerByNetworkID[NetworkID].SetAnimation(x, y);
    }

    public static void DistributeNOInfo(Connection c, packet packet)
    {
        // TODO: Read packet data here
        // var data = packet.Read...();
        string uuid = packet.ReadstringUNICODE();
        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();
        NetworkSystem.instance.FindNetworkObject[uuid].SetMovement(pos, rot);

        // TODO: Handle the packet
    }
}
