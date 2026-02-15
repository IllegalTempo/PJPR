using Steamworks;
using Steamworks.Data;
using System;
using System.Linq;
using UnityEngine;
using static packets;
public class ServerSend
{
    
    public static Result test(NetworkPlayer pl)
    {
        using (packet p = new packet((int)ServerPackets.Test_Packet))
        {
            p.Write(pl.NetworkID);
            p.WriteUNICODE(PacketSend.TestRandomUnicode);
            Debug.Log("sending: " + DateTime.Now.Ticks);
            p.Write(DateTime.Now.Ticks);
            return pl.SendPacket(p);

        }
        ;
    }



    public static Result DistributeMovement(int SourceNetworkID, Vector3 pos, Quaternion headrot, Quaternion bodyrot)
    {
        using (packet p = new packet((int)ServerPackets.DistributeMovement))
        {
            p.Write(SourceNetworkID);
            p.Write(pos);
            p.Write(headrot);
            p.Write(bodyrot);
            return PacketSend.BroadcastPacketToReady(SourceNetworkID, p);

        }
        ;
    }
    public static Result DistributePlayerAnimationState(int SourceNetworkID, float movementx, float movementy)
    {
        using (packet p = new packet((int)ServerPackets.DistributeAnimation))
        {
            p.Write(SourceNetworkID);
            p.Write(movementx);
            p.Write(movementy);

            return PacketSend.BroadcastPacketToReady(SourceNetworkID, p);
        }
        ;
    }

    public static Result NewPlayerJoined(ConnectionInfo newplayer)
    {
        using (packet p = new packet((int)ServerPackets.UpdatePlayerEnterRoomForExistingPlayer))
        {
            p.Write(newplayer.Identity.SteamId);
            p.Write(NetworkSystem.instance.server.GetPlayer(newplayer).NetworkID);
            return PacketSend.BroadcastPacket(newplayer, p);

        }
        ;
    }
    public static Result PlayerQuit(int NetworkID) //who quitted
    {
        using (packet p = new packet((int)ServerPackets.PlayerQuit))
        {
            p.Write(NetworkID);

            return PacketSend.BroadcastPacket(p);

        }
        ;
    }
    public static Result InitRoomInfo(NetworkPlayer target, int NumPlayer)
    {
        using (packet p = new packet((int)ServerPackets.RoomInfoOnPlayerEnterRoom))
        {
            p.Write(NumPlayer);
            for (int i = 0; i < NumPlayer; i++)
            {
                p.Write(NetworkSystem.instance.server.GetSteamID.ElementAt(i).Key);
                p.Write(NetworkSystem.instance.server.GetSteamID[i]); //given information (SteamID)
            }
            return target.SendPacket(p);
        }
    }


    /// <summary>
    /// Update Network Object Info every fixed interval
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pos"></param>
    /// <param name="rot"></param>
    /// <returns></returns>
    public static Result DistributeNOInfo(string id, Vector3 pos, Quaternion rot)
    {
        using (packet p = new packet((int)ServerPackets.DistributeNOInfo))
        {
            p.WriteUNICODE(id);
            p.Write(pos);
            p.Write(rot);

            return PacketSend.BroadcastPacket(p);
        }
    }


    public static Result DistributePickUpItem(string itemid, int PickedUpBy)
    {
        using (packet p = new packet((int)ServerPackets.DistributePickUpItem))
        {


            p.WriteUNICODE(itemid);
            p.Write(PickedUpBy);
            return PacketSend.BroadcastPacket(p);
        }
    }


    public static Result DistributeInitialPos(NetworkPlayer target, Vector3 pos, Quaternion Rot)
    {
        using (packet p = new packet((int)ServerPackets.DistributeInitialPos))
        {


            p.Write(pos);
            p.Write(Rot);
            return target.SendPacket(p);
        }
    }

    /// <summary>
    /// Send Spawn network object to clients. 
    /// </summary>
    /// <param name="prefabID"></param>
    /// <param name="spawnLocation"></param>
    /// <param name="UID">Network Object Identifier</param>
    /// <param name="rot"></param>
    /// <returns></returns>
    public static Result NewObject(string prefabID,string UID, Vector3 spawnLocation, Quaternion rot)
    {
        using (packet p = new packet((int)ServerPackets.NewObject))
        {


            p.WriteUNICODE(prefabID);
            p.WriteUNICODE(UID);
            p.Write(spawnLocation);
            p.Write(rot);

            return PacketSend.BroadcastPacket(p);
        }
    }
}

    
