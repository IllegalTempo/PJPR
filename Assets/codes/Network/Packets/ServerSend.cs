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
            p.Write(pl.steamId);
            p.WriteUNICODE(PacketSend.TestRandomUnicode);
            Debug.Log("sending: " + DateTime.Now.Ticks);
            p.Write(DateTime.Now.Ticks);
            return pl.SendPacket(p);

        }
        ;
    }



    public static Result DistributeMovement(ulong SourceNetworkID, Vector3 pos, Quaternion headrot, Quaternion bodyrot)
    {
        using (packet p = new packet((int)ServerPackets.DistributeMovement))
        {
            p.Write(SourceNetworkID);
            p.Write(pos);
            p.Write(headrot);
            p.Write(bodyrot);
            return PacketSend.BroadcastPacketToReady(p, SourceNetworkID);

        }
        ;
    }
    public static Result DistributePlayerAnimationState(ulong SourceNetworkID, float movementx, float movementy)
    {
        using (packet p = new packet((int)ServerPackets.DistributeAnimation))
        {
            p.Write(SourceNetworkID);
            p.Write(movementx);
            p.Write(movementy);

            return PacketSend.BroadcastPacketToReady(p, SourceNetworkID);
        }
        ;
    }

    //inform the whole room that a new player has joined, and send the new player's steamID.
    public static Result NewPlayerJoined(ConnectionInfo newplayer)
    {
        using (packet p = new packet((int)ServerPackets.UpdatePlayerEnterRoomForExistingPlayer))
        {
            p.Write(newplayer.Identity.SteamId);
            //p.Write(NetworkSystem.instance.server.GetPlayer(newplayer).NetworkID);
            return PacketSend.BroadcastPacket(newplayer, p);

        }
        ;
    }
    public static Result PlayerQuit(ulong steamID) //who quitted
    {
        using (packet p = new packet((int)ServerPackets.PlayerQuit))
        {
            p.Write(steamID);

            return PacketSend.BroadcastPacket(p);

        }
        ;
    }
    public static Result InitRoomInfo(NetworkPlayer target, int NumPlayer)
    {
        using (packet p = new packet((int)ServerPackets.RoomInfoOnPlayerEnterRoom))
        {
            p.Write(NumPlayer);
            p.Write(NetworkSystem.instance.PlayerId);
            foreach (ulong steamid in NetworkSystem.instance.server.players.Keys)
            {
                p.Write(steamid); //given information (SteamID)
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

            return PacketSend.BroadcastPacketToReady(p);
        }
    }


    public static Result DistributePickUpItem(string itemid, ulong PickedUpBy)
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
    public static Result NewObject(string prefabID,string UID, Vector3 spawnLocation, Quaternion rot,ulong owner)
    {
        using (packet p = new packet((int)ServerPackets.NewObject))
        {


            p.WriteUNICODE(prefabID);
            p.WriteUNICODE(UID);
            p.Write(spawnLocation);
            p.Write(rot);
            p.Write(owner);
            return PacketSend.BroadcastPacketToReady(p);
        }
    }

    public static Result DistributeNOactive(string UID,bool status)
    {
        using (packet p = new packet((int)ServerPackets.DistributeNOactive))
        {
            p.WriteUNICODE(UID);
            return PacketSend.BroadcastPacket(p);
        }
    }

    public static Result SyncNetworkObjects(NetworkPlayer target, NetworkObject[] nobjs)
    {
        using (packet p = new packet((int)ServerPackets.SyncNetworkObjects))
        {
            p.Write(nobjs.Length);
            foreach (NetworkObject no in nobjs)
            {

                p.WriteUNICODE(no.Identifier);
                p.Write(no.Owner);
                p.WriteUNICODE(no.PrefabID);
            }
            return target.SendPacket(p);
        }
    }
}

    
