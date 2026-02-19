using Steamworks;
using Steamworks.Data;
using System;
using UnityEngine;
using static packets;


public class ClientSend
{
    //Area for client Packets!
    

    public static Result AnimationState(float movementx, float movementy)
    {
        using (packet p = new packet((int)ClientPackets.SendAnimationState))
        {
            p.Write(movementx);
            p.Write(movementy);
            return SendToServer(p);
        }
    }

    public static Result test()
    {
        using (packet p = new packet((int)ClientPackets.Test_Packet))
        {
            p.WriteUNICODE(PacketSend.TestRandomUnicode);
            p.Write(DateTime.Now.Ticks);
            Debug.Log("sending: " + DateTime.UtcNow.Ticks);

            return SendToServer(p);


        }
        ;
    }

    public static Result ReadyUpdate()
    {
        Debug.Log("Send Ready");
        using (packet p = new packet((int)ClientPackets.Ready))
        {
            p.Write(true);

            return SendToServer(p);


        }
        ;
    }
    public static Result Position(Vector3 pos, Quaternion cameraRotation, Quaternion BodyRotation)
    {
        using (packet p = new packet((int)ClientPackets.SendPosition))
        {
            p.Write(pos);
            p.Write(cameraRotation);
            p.Write(BodyRotation);
            return SendToServer(p);
        }
    }



    public static Result SendNOInfo(string id, Vector3 pos, Quaternion rot)
    {
        using (packet p = new packet((int)ClientPackets.SendNOInfo))
        {
            

            p.WriteUNICODE(id);
            p.Write(pos);
            p.Write(rot);


            return SendToServer(p);
        }
    }


    public static Result PickUpItem(string objectID, ulong whopicked)
    {
        using (packet p = new packet((int)ClientPackets.PickUpItem))
        {
            

            p.WriteUNICODE(objectID);

            p.Write(whopicked);

            return SendToServer(p);
        }
    }
    private static Result SendToServer(packet p)
    {
        Connection server = NetworkSystem.instance.client.GetServer();

        // Fix: Check for default value instead of null for structs
        if (server.Equals(default(Connection)))
        {
            return Result.ConnectFailed;
        }
        else
        {
            return PacketSend.SendPacketToConnection(NetworkSystem.instance.client.GetServer(), p);
        }
    }

    public static Result SendDecorationInteract(string decorationUID)
    {
        using (packet p = new packet((int)ClientPackets.SendDecorationInteract))
        {
            p.WriteUNICODE(decorationUID);
            
            return SendToServer(p);
        }
    }
}
