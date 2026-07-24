using Assets.codes.Network.Packets.BothMessages;
using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Assets.codes.Network.Messages
{
    public static class NetworkSendProfiles
    {
        public const SendType Critical = SendType.Reliable;
        public const SendType State = SendType.Unreliable;
        public const SendType Voice = SendType.Unreliable;
    }

    public class NetworkRouter: MonoBehaviour
    {
        public static NetworkRouter Instance;
        private readonly Queue<Packet> packetQueue = new();
        private uint outgoingSequence;

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
            { (int)packets.BothPackets.NO_Info, NMS_Both_NetworkObjectInfo.Read},
            { (int)packets.BothPackets.NO_Active, NMS_Both_NetworkObjectActive.Read},
            { (int)packets.BothPackets.PickUpItem, NMS_Both_PickUpItem.Read },
            { (int)packets.BothPackets.VoicePacket, NMS_Both_VoicePacket.Read },
            { (int)packets.BothPackets.NO_Slot_Interact, NMS_Both_SlotAttach.Read },
            { (int)packets.BothPackets.SlotDetach, NMS_Both_SlotDetach.Read },
            { (int)packets.BothPackets.QuantityResourceProviderInteract, NMS_Both_MachineInteract.Read },
            { (int)packets.BothPackets.SendCombineItem, NMS_Both_SendCombineItem.Read },
            { (int)packets.BothPackets.Handle_OnReleaseUpdateLevel, NMS_Both_Handle_OnReleaseUpdateLevel.Read },};

        private readonly Dictionary<int, Func<Packet, NMS>> serverMessages = new()
        {
            { (int)packets.ServerPackets.RoomInfoOnPlayerEnterRoom, NMS_Server_SyncPlayer.Read },
            { (int)packets.ServerPackets.UpdatePlayerEnterRoomForExistingPlayer, NMS_Server_NewPlayerJoined.Read },
            { (int)packets.ServerPackets.PlayerQuit, NMS_Server_PlayerQuit.Read },
            { (int)packets.ServerPackets.NewObject, NMS_Server_NewObject.Read },
            { (int)packets.ServerPackets.SyncNetworkObjects, NMS_Server_SyncScene.Read },
            { (int)packets.ServerPackets.StartGameLoop, NMS_Server_StartGameLoop.Read },
            { (int)packets.ServerPackets.NO_Destroy, NMS_Server_NO_Destroy.Read },
            { (int)packets.ServerPackets.StartVotingSession, NMS_Server_StartVotingSession.Read },
            { (int)packets.ServerPackets.VoteResult, NMS_Server_VoteResult.Read },
            { (int)packets.ServerPackets.VoteUpdate, NMS_Server_VoteUpdate.Read },
            { (int)packets.ServerPackets.SpawnMeteorite, NMS_Server_SpawnMeteorite.Read },
            { (int)packets.ServerPackets.DestroyMeteorite, NMS_Server_DestroyMeteorite.Read },
            { (int)packets.ServerPackets.MeteoriteWarning, NMS_Server_MeteoriteWarning.Read },
            { (int)packets.ServerPackets.UpdateWorld_Velocity, NMS_Server_UpdateWorld_Velocity.Read },
            { (int)packets.ServerPackets.UpdateWorld_Rotation, NMS_Server_UpdateWorld_Rotation.Read },
            { (int)packets.ServerPackets.WorldInitBegin, NMS_Server_WorldInitBegin.Read },
            { (int)packets.ServerPackets.WorldInitComplete, NMS_Server_WorldInitComplete.Read },};

        private readonly Dictionary<int, Func<Packet, NMS>> clientMessages = new()
        {
            { (int)packets.ClientPackets.SendReadyState, NMS_Client_ReadyState.Read },
            { (int)packets.ClientPackets.RequestVotingSession, NMS_Client_RequestVotingSession.Read },
            { (int)packets.ClientPackets.CastVote, NMS_Client_CastVote.Read },
            { (int)packets.ClientPackets.RequestWorldState, NMS_Client_RequestWorldState.Read },
        };
        public uint NextOutgoingSequence()
        {
            unchecked
            {
                outgoingSequence++;
                if (outgoingSequence == 0)
                {
                    outgoingSequence = 1;
                }

                return outgoingSequence;
            }
        }

        public void AddToPacketQueue(Packet packet)
        {
            if (packet == null)
            {
                return;
            }

            packetQueue.Enqueue(packet);
        }

        public void UpdateReadyState(ReadyState state)
        {
            int readyState = (int)state;
            NetworkSystem.Instance.initState = readyState;
            SendMessageToServer(new NMS_Client_ReadyState(readyState), NetworkSendProfiles.Critical);
        }

        public Result SendMessageToServer(NMS message, SendType sendType = NetworkSendProfiles.Critical)
        {
            using (Packet p = new Packet(message.PacketID))
            {
                message.Write(p);
                Result result = SendToServer(p, sendType);
                if (result != Result.OK)
                {
                    Debug.LogError($"Failed to send message_{message.PacketID} to server | {result}");
                }

                return result;
            }
        }

        public Result SendMessageToClient(NetworkPlayer player, NMS message, SendType sendType = NetworkSendProfiles.Critical)
        {
            using (Packet p = new Packet(message.PacketID))
            {
                message.Write(p);
                Result result = player.SendPacket(p, sendType);
                if (result != Result.OK)
                {
                    Debug.LogError($"Failed to send message_{message.PacketID} to client {player.SteamName} | {result}");
                }

                return result;
            }
        }

        public Result DistributeMessage(ulong exclude, NMS message, SendType sendType = NetworkSendProfiles.Critical)
        {
            using (Packet p = new Packet(message.PacketID))
            {
                message.Write(p);
                Result result = BroadcastPacket(exclude, p, sendType);
                if (result != Result.OK)
                {
                    Debug.LogError($"Failed to distribute message_{message.PacketID} to clients | {result}");
                }

                return result;
            }
        }

        public Result DistributeMessageToReady(NMS message, ulong exclude = 0, SendType sendType = NetworkSendProfiles.Critical)
        {
            using (Packet p = new Packet(message.PacketID))
            {
                message.Write(p);
                Result result = BroadcastPacketToReady(p,message.GetType().Name, exclude, sendType);
                if (result != Result.OK)
                {
                    Debug.LogError($"Failed to distribute message_{message.PacketID} to ready clients | {result}");
                }

                return result;
            }
        }
        private void Update()
        {
            while (packetQueue.TryDequeue(out var packet))
            {
                try
                {
                    if(NetworkSystem.Instance.IsServer)
                    {
                        OnServerReceivePacket(packet);
                    }
                    else
                    {
                        OnClientReceivePacket(packet);
                    }
                }
                finally
                {
                    packet.Dispose();
                }
            }
        }

        public void OnServerReceivePacket(Packet packet)
        {
            try
            {
                NMS message = GetMessageByPacketID(packet, clientMessages);
                if (message is IServerHandle serverHandle)
                {
                    serverHandle.ServerHandle(packet.sentBy);
                }

                WarnIfUnreadBytes(packet);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to handle server-bound packet id={packet.PacketID} seq={packet.Sequence} from={packet.sentBy?.SteamName ?? "unknown"} size={packet.Length}: {ex}");
            }
        }

        public void OnClientReceivePacket(Packet packet)
        {
            try
            {
                NMS message = GetMessageByPacketID(packet, serverMessages);
                if (message is IClientHandle clientHandle)
                {
                    clientHandle.ClientHandle();
                }

                WarnIfUnreadBytes(packet);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to handle client-bound packet id={packet.PacketID} seq={packet.Sequence} size={packet.Length}: {ex}");
            }
        }

        private NMS GetMessageByPacketID(Packet packet, Dictionary<int, Func<Packet, NMS>> directionMessages)
        {
            int packetID = packet.PacketID;
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

        private void WarnIfUnreadBytes(Packet packet)
        {
            if (packet.BytesRemaining > 0)
            {
                Debug.LogWarning($"Packet id={packet.PacketID} seq={packet.Sequence} had {packet.BytesRemaining} unread byte(s).");
            }
        }

        private Result SendToServer(Packet p, SendType sendType = NetworkSendProfiles.Critical)
        {
            Connection server = NetworkSystem.Instance.GetServerConnection();
            if (server.Equals(default(Connection)))
            {
                return Result.ConnectFailed;
            }

            return SendPacketToConnection(server, p, sendType);
        }

        public Result BroadcastPacket(Packet p)
        {
            return BroadcastPacket(9999, p);
        }

        public Result BroadcastPacket(ConnectionInfo info, Packet p, SendType sendType = NetworkSendProfiles.Critical)
        {
            return BroadcastPacket(info.Identity.SteamId, p, sendType);
        }

        public Result BroadcastPacket(ulong excludeid, Packet p, SendType sendType = NetworkSendProfiles.Critical)
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

                if (player.SendPacket(p, sendType) != Result.OK)
                {
                    Debug.Log("Result Error when broadcasting packet");
                    return Result.Cancelled;
                }
            }

            return Result.OK;
        }

        public Result BroadcastPacketToReady(Packet p,string messagename, ulong excludeid = 0, SendType sendType = NetworkSendProfiles.Critical)
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

                if (player.SendPacket(p, sendType) != Result.OK)
                {
                    Debug.Log("Result Error when broadcasting packet");
                    return Result.Cancelled;
                }
            }

            return Result.OK;
        }

        public Result SendPacketToConnection(Connection c, Packet p, SendType sendType = NetworkSendProfiles.Critical)
        {
            byte[] data = p.GetPacketData(NextOutgoingSequence());
            IntPtr datapointer = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, datapointer, data.Length);
            Result result = c.SendMessage(datapointer, data.Length, sendType);
            Marshal.FreeHGlobal(datapointer);
            return result;
        }
    }
}
