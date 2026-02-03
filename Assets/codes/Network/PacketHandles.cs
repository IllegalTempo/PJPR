using Steamworks.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class PacketHandles_Method
{
	public static void Server_Handle_test(NetworkPlayer p, packet packet)
	{
		string text = packet.ReadstringUNICODE();
		long clienttime = packet.Readlong();
		if (text == PacketSend.TestRandomUnicode)
		{
			Debug.Log($"{clienttime} Confirmed {p.SteamName}, successfully connected, delay:{(DateTime.Now.Ticks - clienttime) / 10000}ms");
			//trigger listeners
			NetworkListener.Server_OnPlayerJoinSuccessful?.Invoke(p);

		}
		else
		{
			Debug.Log($"Check Code Mismatched Client Message: {text}");
		}
	}

	public static void Server_Handle_AnimationState(NetworkPlayer p, packet packet)
	{
		float movex = packet.Readfloat();
		float movey = packet.Readfloat();
		p.player.SetAnimation(movex, movey);
		PacketSend.Server_DistributePlayerAnimationState(p.NetworkID, movex, movey);
	}
	public static void Server_Handle_ReadyUpdate(NetworkPlayer p, packet packet)
	{
		bool ready = packet.Readbool();
		p.MovementUpdateReady = ready;
		Debug.Log($"Player {p.SteamName} is ready for receiving pos informations!");
	}
	public static void Server_Handle_PosUpdate(NetworkPlayer p, packet packet)
	{
		Vector3 pos = packet.Readvector3();
		Quaternion rot = packet.Readquaternion();
		Quaternion yrot = packet.Readquaternion();
		p.player.SetMovement(pos, rot, yrot);
		PacketSend.Server_DistributeMovement(p.NetworkID, pos, rot, yrot);
	}



	public static async void Client_Handle_test(Connection c, packet packet)
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
		PacketSend.Client_Send_test();
	}
	public static async void Client_Handle_InitRoomInfo(Connection c, packet packet)
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
		PacketSend.Client_Send_ReadyUpdate();
	}
	public static void Client_Handle_NewPlayerJoin(Connection c, packet packet)
	{
		ulong playerid = packet.Readulong();
		int supposeNetworkID = packet.Readint();




		NetworkSystem.instance.client.GetPlayerByNetworkID.Add(supposeNetworkID, NetworkSystem.instance.SpawnPlayer(false, supposeNetworkID, playerid));
	}
	public static void Client_Handle_PlayerQuit(Connection c, packet packet)
	{
		GameClient cl = NetworkSystem.instance.client;
		int NetworkID = packet.Readint();
		cl.GetPlayerByNetworkID[NetworkID].Disconnect();
		cl.GetPlayerByNetworkID.Remove(NetworkID);
	}

	public static void Client_Handle_ReceivedPlayerMovement(Connection c, packet packet)
	{
		int NetworkID = packet.Readint();

		Vector3 pos = packet.Readvector3();
		Quaternion headrot = packet.Readquaternion();
		Quaternion bodyrot = packet.Readquaternion();
		NetworkSystem.instance.client.GetPlayerByNetworkID[NetworkID].SetMovement(pos, headrot, bodyrot);
	}


	public static void Client_Handle_ReceivedPlayerAnimation(Connection c, packet packet)
	{
		int NetworkID = packet.Readint();
		float x = packet.Readfloat();
		float y = packet.Readfloat();
		NetworkSystem.instance.client.GetPlayerByNetworkID[NetworkID].SetAnimation(x, y);
	}

	public static void Client_Handle_DistributeNOInfo(Connection c, packet packet)
	{
		// TODO: Read packet data here
		// var data = packet.Read...();
		string uuid = packet.ReadstringUNICODE();
		Vector3 pos = packet.Readvector3();
		Quaternion rot = packet.Readquaternion();
		NetworkSystem.instance.FindNetworkObject[uuid].SetMovement(pos, rot);

		// TODO: Handle the packet
	}

	public static void Server_Handle_SendNOInfo(NetworkPlayer p, packet packet)
	{
		string uuid = packet.ReadstringUNICODE();
		Vector3 pos = packet.Readvector3();
		Quaternion rot = packet.Readquaternion();
		NetworkSystem.instance.FindNetworkObject[uuid].SetMovement(pos, rot);
		PacketSend.Server_Send_DistributeNOInfo(uuid,pos,rot);


	}

	public static void Server_Handle_PickUpItem(NetworkPlayer p, packet packet)
	{
		// TODO: Read packet data here
		// var data = packet.Read...();
		
		// TODO: Handle the packet
		string itemid = packet.ReadstringUNICODE();
		int whopicked = p.NetworkID;
			NetworkSystem.instance.FindNetworkObject[itemid].Owner = whopicked;

		PacketSend.Server_Send_DistributePickUpItem(itemid,p.NetworkID);
	}

	public static void Client_Handle_DistributePickUpItem(Connection c, packet packet)
	{
		// TODO: Read packet data here
		// var data = packet.Read...();
		string itemid = packet.ReadstringUNICODE();
		int whopicked = packet.Readint();

			NetworkSystem.instance.FindNetworkObject[itemid].Owner = whopicked;

		// TODO: Handle the packet
	}

	

	public static void Client_Handle_DistributeInitialPos(Connection c, packet packet)
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
}


