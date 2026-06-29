using Assets.codes.Network.Packets.BothMessages;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public class NetworkRouter: MonoBehaviour
    {
        public static NetworkRouter Instance;
        private void Awake()
        {
            Instance = this;
        }

        public static string TestRandomUnicode = "幻想鄉是一個與外界隔絕的神秘之地，其存在自古以來便被視為傳說而流傳。";

        private readonly Dictionary<int, Func<Packet, NMS>> bothMessages = new()
        {
            { (int)packets.BothPackets.Test, NMS_Both_TestPacket.Read },
            { (int)packets.BothPackets.PosUpdate, NMS_Both_PositionUpdate.Read },
            { (int)packets.BothPackets.PlayerAnimation, NMS_Both_PlayerAnimation.Read },
            { (int)packets.BothPackets.Interact, NMS_Both_Interact.Read },
            { (int)packets.BothPackets.NO_Info, NMS_Both_NetworkObjectInfo.Read},
            { (int)packets.BothPackets.NO_Active, NMS_Both_NetworkObjectActive.Read},
            { (int)packets.BothPackets.PickUpItem, NMS_Both_PickUpItem.Read },
            { (int)packets.BothPackets.VoicePacket, NMS_Both_VoicePacket.Read },
            { (int)packets.BothPackets.NO_Slot_Interact, NMS_Both_SlotAttach.Read },
            { (int)packets.BothPackets.SlotDetach, NMS_Both_SlotDetach.Read },};

        private readonly Dictionary<int, Func<Packet, NMS>> serverMessages = new()
        {
            { (int)packets.ServerPackets.RoomInfoOnPlayerEnterRoom, NMS_Server_SyncPlayer.Read },
            { (int)packets.ServerPackets.UpdatePlayerEnterRoomForExistingPlayer, NMS_Server_NewPlayerJoined.Read },
            { (int)packets.ServerPackets.PlayerQuit, NMS_Server_PlayerQuit.Read },
            { (int)packets.ServerPackets.NewObject, NMS_Server_NewObject.Read },
            { (int)packets.ServerPackets.SyncNetworkObjects, NMS_Server_SyncNetworkPrefab.Read },
            { (int)packets.ServerPackets.SendMissionInfo, NMS_Server_MissionInfo.Read },
            { (int)packets.ServerPackets.StartGameLoop, NMS_Server_StartGameLoop.Read }
        ,
            { (int)packets.ServerPackets.NO_Destroy, NMS_Server_NO_Destroy.Read },};

        private readonly Dictionary<int, Func<Packet, NMS>> clientMessages = new()
        {
            { (int)packets.ClientPackets.SendReadyState, NMS_Client_ReadyState.Read },
        };
        public void UpdateReadyState(ReadyState state)
        {
            int readyState = (int)state;
            NetworkSystem.Instance.initState = readyState;
            SendMessageToServer(new NMS_Client_ReadyState(readyState));
        }
        public Result SendMessageToServer(NMS message)
        {
            using (Packet p = new Packet(message.PacketID))
            {
                message.Write(p);
                Result result = SendToServer(p);
                if (result != Result.OK)
                {
                    Debug.LogError($"Failed to send message_{message.PacketID} to server | {result}");
                }

                return result;
            }
        }

        public Result SendMessageToClient(NetworkPlayer player, NMS message)
        {
            using (Packet p = new Packet(message.PacketID))
            {
                message.Write(p);
                Result result = player.SendPacket(p);
                if (result != Result.OK)
                {
                    Debug.LogError($"Failed to send message_{message.PacketID} to client {player.SteamName} | {result}");
                }

                return result;
            }
        }

        public Result DistributeMessage(ulong exclude, NMS message)
        {
            using (Packet p = new Packet(message.PacketID))
            {
                message.Write(p);
                Result result = BroadcastPacket(exclude, p);
                if (result != Result.OK)
                {
                    Debug.LogError($"Failed to distribute message_{message.PacketID} to clients | {result}");
                }

                return result;
            }
        }

        public Result DistributeMessageToReady(NMS message, ulong exclude = 0)
        {
            using (Packet p = new Packet(message.PacketID))
            {
                message.Write(p);
                Result result = BroadcastPacketToReady(p, exclude);
                if (result != Result.OK)
                {
                    Debug.LogError($"Failed to distribute message_{message.PacketID} to ready clients | {result}");
                }

                return result;
            }
        }

        public void OnServerReceivePacket(Packet packet, NetworkPlayer player)
        {
            NMS message = GetMessageByPacketID(packet, clientMessages);
            if (message is IServerHandle serverHandle)
            {
                serverHandle.ServerHandle(player);
            }
        }

        public void OnClientReceivePacket(Packet packet)
        {
            NMS message = GetMessageByPacketID(packet, serverMessages);
            if (message is IClientHandle clientHandle)
            {
                clientHandle.ClientHandle();
            }
        }

        private NMS GetMessageByPacketID(Packet packet, Dictionary<int, Func<Packet, NMS>> directionMessages)
        {
            int packetID = packet.Readint();
            if (directionMessages.TryGetValue(packetID, out Func<Packet, NMS> directionFactory))
            {
                return directionFactory(packet);
            }

            if (bothMessages.TryGetValue(packetID, out Func<Packet, NMS> bothFactory))
            {
                return bothFactory(packet);
            }

            Debug.LogError($"No NMS found for packet ID {packetID}");
            return null;
        }

        private Result SendToServer(Packet p)
        {
            Connection server = NetworkSystem.Instance.GetServerConnection();
            if (server.Equals(default(Connection)))
            {
                return Result.ConnectFailed;
            }

            return SendPacketToConnection(server, p);
        }

        public Result BroadcastPacket(Packet p)
        {
            return BroadcastPacket(9999, p);
        }

        public Result BroadcastPacket(ConnectionInfo info, Packet p)
        {
            return BroadcastPacket(info.Identity.SteamId, p);
        }

        public Result BroadcastPacket(ulong excludeid, Packet p)
        {
            if (NetworkSystem.Instance == null || NetworkSystem.Instance.Server == null || NetworkSystem.Instance.Server.NetworkUsers == null)
            {
                return Result.Disabled;
            }

            foreach (NetworkPlayer player in NetworkSystem.Instance.Server.NetworkUsers.Values)
            {
                if (player == null || player.steamId == excludeid)
                {
                    continue;
                }

                if (player.SendPacket(p) != Result.OK)
                {
                    Debug.Log("Result Error when broadcasting packet");
                    return Result.Cancelled;
                }
            }

            return Result.OK;
        }

        public Result BroadcastPacketToReady(Packet p, ulong excludeid = 0)
        {
            if (NetworkSystem.Instance == null || NetworkSystem.Instance.Server == null || NetworkSystem.Instance.Server.NetworkUsers == null)
            {
                return Result.Disabled;
            }

            foreach (NetworkPlayer player in NetworkSystem.Instance.Server.NetworkUsers.Values)
            {
                if (player == null || player.steamId == excludeid || player.ReadyState != (int)ReadyState.SyncNetworkObjects)
                {
                    continue;
                }

                if (player.SendPacket(p) != Result.OK)
                {
                    Debug.Log("Result Error when broadcasting packet");
                    return Result.Cancelled;
                }
            }

            return Result.OK;
        }

        public Result SendPacketToConnection(Connection c, Packet p)
        {
            byte[] data = p.GetPacketData();
            IntPtr datapointer = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, datapointer, data.Length);
            Result result = c.SendMessage(datapointer, data.Length, SendType.Reliable);
            Marshal.FreeHGlobal(datapointer);
            return result;
        }
    }
}
