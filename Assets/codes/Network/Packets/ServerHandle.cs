using Assets.codes.items;
using System;
using UnityEngine;

public class ServerHandle
{
    public static void SendNOInfo(NetworkPlayer p, packet packet)
    {
        string uuid = packet.ReadstringUNICODE();
        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();
        NetworkSystem.INSTANCE.FindNetworkObject[uuid].SetServerMovement(pos, rot);
        ServerSend.DistributeNOInfo(uuid, pos, rot);


    }

    public static void PickUpItem(NetworkPlayer p, packet packet)
    {




        string itemid = packet.ReadstringUNICODE();
        ulong whopicked = packet.Readulong();
        NetworkSystem.INSTANCE.FindNetworkObject[itemid].gameObject.GetComponent<Item>().Network_onChangeOwnership(whopicked);

        ServerSend.DistributePickUpItem(itemid, whopicked);
    }
    public static void test(NetworkPlayer p, packet packet)
    {
        string text = packet.ReadstringUNICODE();
        long clienttime = packet.Readlong();
        if (text == PacketSend.TestRandomUnicode)
        {
            Debug.Log($"{clienttime} Confirmed {p.SteamName}, successfully connected, delay:{(DateTime.UtcNow.Ticks - clienttime) / 10000}ms");
            //trigger listeners
            NetworkListener.Server_OnPlayerJoinSuccessful?.Invoke(p);

        }
        else
        {
            Debug.Log($"Check Code Mismatched Client Message: {text}");
        }
    }

    public static void AnimationState(NetworkPlayer p, packet packet)
    {
        float movex = packet.Readfloat();
        float movey = packet.Readfloat();
        p.player.SetAnimation(movex, movey);
        ServerSend.DistributePlayerAnimationState(p.steamId, movex, movey);

    }
    public static void ReadyUpdate(NetworkPlayer p, packet packet)
    {
        bool ready = packet.Readbool();
        p.onReady(ready);
        Debug.Log($"Player {p.SteamName} is ready for receiving pos informations!");
    }
    public static void PosUpdate(NetworkPlayer p, packet packet)
    {
        Vector3 pos = packet.Readvector3();
        Quaternion rot = packet.Readquaternion();
        Quaternion yrot = packet.Readquaternion();
        p.player.SetMovement(pos, rot, yrot);
        ServerSend.DistributeMovement(p.steamId, pos, rot, yrot);
    }

    public static void SendDecorationInteract(NetworkPlayer p, packet packet)
    {
        string decorationUID = packet.ReadstringUNICODE();
        IUsable decoration = NetworkSystem.INSTANCE.FindNetworkObject[decorationUID].GetComponent<IUsable>();
        PlayerMain who = p.player.playerControl;
        decoration.OnInteract(who);
        ServerSend.DistributeDecorationInteract(p.steamId, decorationUID);
    }
}

